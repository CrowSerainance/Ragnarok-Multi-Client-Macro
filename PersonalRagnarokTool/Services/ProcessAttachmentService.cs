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

    public string LastInjectionFailure { get; private set; } = "";

    /// <summary>Inject a DLL into the target process via CreateRemoteThread + LoadLibraryA.</summary>
    public bool InjectDll(int processId, string dllPath)
    {
        return InjectViaShadowThread(processId, dllPath);
    }

    /// <summary>Stealth injection (same mechanism, uses hijacked handle if available).</summary>
    public bool InjectDllStealth(int processId, string dllPath)
    {
        return InjectViaShadowThread(processId, dllPath);
    }

    private bool InjectViaShadowThread(int targetPid, string dllPath)
    {
        LastInjectionFailure = "";
        bool useReadHandle = _readHandle != IntPtr.Zero && _handleFromLaunch;
        IntPtr hProcess = useReadHandle ? _readHandle : NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, (uint)targetPid);
        if (hProcess == IntPtr.Zero && _readHandle != IntPtr.Zero)
            hProcess = _readHandle;
        if (hProcess == IntPtr.Zero)
        {
            LastInjectionFailure = $"OpenProcess failed (Win32={Marshal.GetLastWin32Error()}).";
            return false;
        }

        IntPtr pLoadLibrary = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (pLoadLibrary == IntPtr.Zero)
        {
            if (!useReadHandle && hProcess != _readHandle) NativeMethods.CloseHandle(hProcess);
            LastInjectionFailure = "GetProcAddress(LoadLibraryA) failed.";
            return false;
        }

        byte[] pathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
        IntPtr remoteMem = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero)
        {
            if (!useReadHandle && hProcess != _readHandle) NativeMethods.CloseHandle(hProcess);
            LastInjectionFailure = $"VirtualAllocEx failed (Win32={Marshal.GetLastWin32Error()}).";
            return false;
        }

        bool writeOk = NativeMethods.WriteProcessMemory(hProcess, remoteMem, pathBytes, (uint)pathBytes.Length, out _);
        if (!writeOk)
        {
            NativeMethods.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
            if (!useReadHandle && hProcess != _readHandle) NativeMethods.CloseHandle(hProcess);
            LastInjectionFailure = $"WriteProcessMemory failed (Win32={Marshal.GetLastWin32Error()}).";
            return false;
        }

        IntPtr hThread = NativeMethods.CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, remoteMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            uint rt = NativeMethods.RtlCreateUserThread(hProcess, IntPtr.Zero, false, 0, IntPtr.Zero, IntPtr.Zero, pLoadLibrary, remoteMem, out hThread, out _);
            if (rt != 0 || hThread == IntPtr.Zero)
            {
                NativeMethods.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
                if (!useReadHandle && hProcess != _readHandle) NativeMethods.CloseHandle(hProcess);
                LastInjectionFailure = $"CreateRemoteThread+RtlCreateUserThread failed (Win32={Marshal.GetLastWin32Error()}).";
                return false;
            }
        }

        NativeMethods.CloseHandle(hThread);
        if (!useReadHandle && hProcess != _readHandle) NativeMethods.CloseHandle(hProcess);
        return true;
    }

    /// <summary>
    /// Watch flow: suspend all threads, inject ColdHide (optional) + HEAVENSGATE + Dll1,
    /// connect pipe, then resume. Identical to Simple Ragnarok Program's workflow.
    /// </summary>
    public async Task<bool> AttachSuspendInjectBothConnectResumeAsync(
        int targetPid, string heavensGatePath, string dll1Path,
        Func<Task<bool>> connectPipeAsync, string? coldHidePath = null)
    {
        LastInjectionFailure = "";
        IntPtr hProcess = NativeMethods.OpenProcess(NativeMethods.PROCESS_ALL_ACCESS, false, (uint)targetPid);
        if (hProcess == IntPtr.Zero)
        {
            LastInjectionFailure = $"OpenProcess failed (Win32={Marshal.GetLastWin32Error()}).";
            return false;
        }
        var threads = SuspendOrResumeAllThreads((uint)targetPid, true);
        try
        {
            if (!string.IsNullOrEmpty(coldHidePath) && System.IO.File.Exists(coldHidePath))
            {
                if (!InjectOneDll(hProcess, coldHidePath)) return false;
                System.Threading.Thread.Sleep(100);
            }
            if (!InjectOneDll(hProcess, heavensGatePath)) return false;
            System.Threading.Thread.Sleep(50);
            if (!InjectOneDll(hProcess, dll1Path)) return false;
            System.Threading.Thread.Sleep(250);
            bool pipeOk = await connectPipeAsync();
            if (!pipeOk) LastInjectionFailure = "Pipe connect failed (Dll1 may not have created it).";
        }
        finally
        {
            SuspendOrResumeAllThreads((uint)targetPid, false, threads);
        }
        _readHandle = hProcess;
        _process = Process.GetProcessById(targetPid);
        _handleFromLaunch = false;
        try { _baseAddress = _process.MainModule?.BaseAddress ?? IntPtr.Zero; } catch { _baseAddress = IntPtr.Zero; }
        _isAttached = true;
        AttachStateChanged?.Invoke();
        return true;
    }

    private bool InjectOneDll(IntPtr hProcess, string dllPath)
    {
        IntPtr pLoadLibrary = NativeMethods.GetProcAddress(NativeMethods.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (pLoadLibrary == IntPtr.Zero) { LastInjectionFailure = "GetProcAddress failed."; return false; }
        byte[] pathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
        IntPtr remoteMem = NativeMethods.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length, NativeMethods.MEM_COMMIT | NativeMethods.MEM_RESERVE, NativeMethods.PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero) { LastInjectionFailure = "VirtualAllocEx failed."; return false; }
        if (!NativeMethods.WriteProcessMemory(hProcess, remoteMem, pathBytes, (uint)pathBytes.Length, out _))
        {
            NativeMethods.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
            LastInjectionFailure = "WriteProcessMemory failed.";
            return false;
        }
        IntPtr hThread = NativeMethods.CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, remoteMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            uint rt = NativeMethods.RtlCreateUserThread(hProcess, IntPtr.Zero, false, 0, IntPtr.Zero, IntPtr.Zero, pLoadLibrary, remoteMem, out hThread, out _);
            if (rt != 0 || hThread == IntPtr.Zero)
            {
                NativeMethods.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
                LastInjectionFailure = "CreateRemoteThread failed.";
                return false;
            }
        }
        NativeMethods.CloseHandle(hThread);
        return true;
    }

    private List<IntPtr> SuspendOrResumeAllThreads(uint processId, bool suspend, List<IntPtr>? existingHandles = null)
    {
        var handles = new List<IntPtr>();
        if (!suspend && existingHandles != null)
        {
            foreach (var h in existingHandles)
            {
                NativeMethods.ResumeThread(h);
                NativeMethods.CloseHandle(h);
            }
            return handles;
        }
        IntPtr snap = NativeMethods.CreateToolhelp32Snapshot(NativeMethods.TH32CS_SNAPTHREAD, 0);
        if (snap == IntPtr.Zero || snap.ToInt64() == -1) return handles;
        try
        {
            var te = new NativeMethods.THREADENTRY32 { dwSize = (uint)Marshal.SizeOf<NativeMethods.THREADENTRY32>() };
            if (!NativeMethods.Thread32First(snap, ref te)) return handles;
            do
            {
                if (te.th32OwnerProcessID != processId) continue;
                IntPtr hThread = NativeMethods.OpenThread(NativeMethods.THREAD_SUSPEND_RESUME, false, te.th32ThreadID);
                if (hThread == IntPtr.Zero) continue;
                NativeMethods.SuspendThread(hThread);
                handles.Add(hThread);
            } while (NativeMethods.Thread32Next(snap, ref te));
        }
        finally
        {
            NativeMethods.CloseHandle(snap);
        }
        return handles;
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
