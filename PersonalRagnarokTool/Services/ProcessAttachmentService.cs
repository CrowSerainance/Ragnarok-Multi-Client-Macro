using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PersonalRagnarokTool.Services;

public sealed class ProcessAttachmentService
{
    private IntPtr _readHandle = IntPtr.Zero;
    private IntPtr _writeHandle = IntPtr.Zero;
    private bool _handleFromLaunch;
    private Process? _process;
    private IntPtr _baseAddress = IntPtr.Zero;
    private bool _isAttached;

    public IntPtr DriverHandle { get; private set; } = IntPtr.Zero;

    public bool IsAttached => _isAttached && _process != null && !_process.HasExited;
    public Process? Process => _process;

    public IntPtr Handle => _readHandle;

    public IntPtr WriteHandle
    {
        get
        {
            if (_handleFromLaunch)
                return _readHandle;

            if (_writeHandle == IntPtr.Zero && _process != null && !_process.HasExited)
            {
                uint writeAccess = NativeMethods.PROCESS_VM_WRITE | NativeMethods.PROCESS_VM_OPERATION;
                _writeHandle = NativeMethods.OpenProcess(writeAccess, false, (uint)_process.Id);
            }
            return _writeHandle;
        }
    }

    public int ProcessId => _process?.Id ?? 0;
    public IntPtr BaseAddress => _baseAddress;
    public IntPtr WindowHandle { get; private set; } = IntPtr.Zero;
    public string LastAttachFailure { get; private set; } = "";
    
    public event Action? AttachStateChanged;

    public void CloseWriteHandle()
    {
        if (_handleFromLaunch) return;
        if (_writeHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_writeHandle);
            _writeHandle = IntPtr.Zero;
        }
    }

    public bool Attach(int processId, IntPtr windowHandle)
    {
        Detach();
        WindowHandle = windowHandle;
        try
        {
            var proc = Process.GetProcessById(processId);
            return AttachToProcess(proc);
        }
        catch
        {
            Detach();
            return false;
        }
    }

    private bool AttachToProcess(Process proc)
    {
        try
        {
            _process = proc;
            uint readAccess = NativeMethods.PROCESS_VM_READ | NativeMethods.PROCESS_QUERY_INFORMATION;
            LastAttachFailure = "";
            _readHandle = NativeMethods.OpenProcess(readAccess, false, (uint)_process.Id);
            
            if (_readHandle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                
                var clientId = new NativeMethods.CLIENT_ID
                {
                    UniqueProcess = (IntPtr)_process.Id,
                    UniqueThread = IntPtr.Zero
                };
                var objAttributes = new NativeMethods.OBJECT_ATTRIBUTES();
                objAttributes.Length = Marshal.SizeOf(typeof(NativeMethods.OBJECT_ATTRIBUTES));

                int nt = NativeMethods.NtOpenProcess(out var hProcess, readAccess, ref objAttributes, ref clientId);
                if (nt == NativeMethods.STATUS_SUCCESS && hProcess != IntPtr.Zero)
                {
                    _readHandle = hProcess;
                }
                else
                {
                    if (!AttachViaHandleHijacking(proc.Id))
                    {
                        string dllPath = GetOptionalSupportFilePath("Support", "Injection", "FsmEngine.dll");
                        if (!System.IO.File.Exists(dllPath) || !AttachViaShadowThreadLoader(proc.Id, dllPath))
                        {
                            string sysPath = GetOptionalSupportFilePath("Support", "Kernel", "AbyssGate.sys");
                            if (!System.IO.File.Exists(sysPath) || !StartAbyssGateDriver(sysPath))
                            {
                                LastAttachFailure = $"Win32={err}, NTSTATUS=0x{nt:X8}";
                                if (nt == unchecked((int)0xC0000022)) LastAttachFailure += " (ACCESS_DENIED)";
                                _process = null;
                                return false;   
                            }
                        }
                    }
                }
            }
            
            // Store BaseAddress safely at attach time before any Gepard hooks can block module enumeration
            try { _baseAddress = proc.MainModule?.BaseAddress ?? IntPtr.Zero; } catch { _baseAddress = IntPtr.Zero; }

            _isAttached = true;
            AttachStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            LastAttachFailure = $"Exception: {ex.Message}";
            Detach();
            return false;
        }
    }

    private bool AttachViaHandleHijacking(int targetPid)
    {
        int currentLength = 0x100000;
        IntPtr ptr = Marshal.AllocHGlobal(currentLength);
        try
        {
            int status;
            while ((status = NativeMethods.NtQuerySystemInformation(NativeMethods.SystemExtendedHandleInformation, ptr, currentLength, out _)) == NativeMethods.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ptr);
                currentLength *= 2;
                ptr = Marshal.AllocHGlobal(currentLength);
            }

            if (status != NativeMethods.STATUS_SUCCESS) return false;

            int ptrSize = IntPtr.Size;
            long handleCount = ptrSize == 8 ? Marshal.ReadInt64(ptr) : Marshal.ReadInt32(ptr);
            IntPtr handlePtr = IntPtr.Add(ptr, ptrSize * 2);
            int structSize = Marshal.SizeOf(typeof(NativeMethods.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

            IntPtr currentProcess = NativeMethods.GetCurrentProcess();
            uint myPid = NativeMethods.GetProcessId(currentProcess);

            for (long i = 0; i < handleCount; i++)
            {
                var entry = Marshal.PtrToStructure<NativeMethods.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);
                handlePtr = IntPtr.Add(handlePtr, structSize);

                uint sourcePid = (uint)entry.UniqueProcessId.ToUInt64();

                if (sourcePid == myPid || sourcePid == (uint)targetPid || sourcePid == 0 || sourcePid == 4)
                    continue;

                uint access = entry.GrantedAccess;
                if ((access & NativeMethods.PROCESS_VM_READ) == 0 && access != NativeMethods.PROCESS_ALL_ACCESS && access != 0x1FFFFF)
                    continue;

                if (access == 0x00100000 || access == 0x001F0001 || access == 0x0012019F || access == 0x00120089)
                    continue;

                IntPtr sourceProc = NativeMethods.OpenProcess(NativeMethods.PROCESS_DUP_HANDLE, false, sourcePid);
                if (sourceProc == IntPtr.Zero) continue;

                IntPtr handleValue = (IntPtr)(long)entry.HandleValue.ToUInt64();

                if (NativeMethods.DuplicateHandle(sourceProc, handleValue, currentProcess, out IntPtr dupHandle, 0, false, NativeMethods.DUPLICATE_SAME_ACCESS))
                {
                    try
                    {
                        uint dupTargetPid = NativeMethods.GetProcessId(dupHandle);
                        if (dupTargetPid == (uint)targetPid)
                        {
                            byte[] testBuf = new byte[1];
                            NativeMethods.ReadProcessMemory(dupHandle, IntPtr.Zero, testBuf, 1, out _);

                            _readHandle = dupHandle;
                            NativeMethods.CloseHandle(sourceProc);

                            if (access == NativeMethods.PROCESS_ALL_ACCESS || access == 0x1FFFFF ||
                                (access & NativeMethods.PROCESS_VM_WRITE) == NativeMethods.PROCESS_VM_WRITE)
                            {
                                _handleFromLaunch = true;
                            }

                            return true;
                        }
                    }
                    catch { }

                    NativeMethods.CloseHandle(dupHandle);
                }
                NativeMethods.CloseHandle(sourceProc);
            }
        }
        catch { }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return false;
    }

    private bool AttachViaShadowThreadLoader(int targetPid, string dllPath)
    {
        IntPtr hProcess = _readHandle != IntPtr.Zero ? _readHandle : NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, (uint)targetPid);
        if (hProcess == IntPtr.Zero) return false;

        IntPtr pLoadLibrary = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (pLoadLibrary == IntPtr.Zero) return false;

        byte[] pathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
        IntPtr remoteMem = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero) return false;

        bool writeOk = NativeMethods.WriteProcessMemory(hProcess, remoteMem, pathBytes, (uint)pathBytes.Length, out _);
        if (writeOk)
        {
            IntPtr hThread = NativeMethods.CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, remoteMem, 0, IntPtr.Zero);
            if (hThread != IntPtr.Zero) return true; 
        }
        NativeMethods.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
        return false;
    }

    private bool StartAbyssGateDriver(string driverPath)
    {
        DriverHandle = NativeMethods.CreateFile(@"\\.\AbyssGate", NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, 0, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        if (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1) return true;

        IntPtr scm = NativeMethods.OpenSCManager(null, null, NativeMethods.SC_MANAGER_ALL_ACCESS);
        if (scm == IntPtr.Zero) return false;

        string serviceName = "AbyssGate";
        IntPtr hService = NativeMethods.OpenService(scm, serviceName, NativeMethods.SERVICE_ALL_ACCESS);
        if (hService == IntPtr.Zero)
        {
            hService = NativeMethods.CreateService(scm, serviceName, "AbyssGate Kernel Driver", NativeMethods.SERVICE_ALL_ACCESS, 
                NativeMethods.SERVICE_KERNEL_DRIVER, NativeMethods.SERVICE_DEMAND_START, NativeMethods.SERVICE_ERROR_NORMAL, 
                driverPath, null, IntPtr.Zero, null, null, null);
        }

        bool started = false;
        if (hService != IntPtr.Zero)
        {
            started = NativeMethods.StartService(hService, 0, null);
            NativeMethods.CloseServiceHandle(hService);
        }
        NativeMethods.CloseServiceHandle(scm);

        DriverHandle = NativeMethods.CreateFile(@"\\.\AbyssGate", NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE, 0, IntPtr.Zero, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        return DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1;
    }

    private static string GetOptionalSupportFilePath(params string[] pathParts)
    {
        string baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, Path.Combine(pathParts));
    }

    public void Detach()
    {
        CloseWriteHandle();
        if (_readHandle != IntPtr.Zero && !_handleFromLaunch)
        {
            NativeMethods.CloseHandle(_readHandle);
            _readHandle = IntPtr.Zero;
        }
        if (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1)
        {
            NativeMethods.CloseHandle(DriverHandle);
            DriverHandle = IntPtr.Zero;
        }
        _isAttached = false;
        _baseAddress = IntPtr.Zero;
        _handleFromLaunch = false;
        _process = null;
        WindowHandle = IntPtr.Zero;
        LastAttachFailure = "";
        AttachStateChanged?.Invoke();
    }
}
