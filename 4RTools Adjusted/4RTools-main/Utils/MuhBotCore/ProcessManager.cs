#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace _4RTools.Utils.MuhBotCore;

public record ProcessDisplayItem(string ProcessName, int Pid, string DisplayText)
{
    public override string ToString() => DisplayText;
}

public class ProcessManager
{
    private static readonly string[] CommonProcessNames =
    {
        "Muh", "Ragexe", "ragexe", "cRagexe",
        "2025-06-04_Ragexe_1337_patched", "Ragnarok"
    };

    /// <summary>Default client executable path for MuhRO.</summary>
    public const string MuhRoExePath = @"E:\RAGNAROK ONLINE\MuhRO\Muh.exe";

    /// <summary>Default game folder for MuhRO (where proxy DLL is placed).</summary>
    public const string MuhRoGameFolder = @"E:\RAGNAROK ONLINE\MuhRO";

    private static readonly HashSet<int> _attachedPids = new();
    private static readonly object _pidLock = new();

    private IntPtr _readHandle = IntPtr.Zero;
    private IntPtr _writeHandle = IntPtr.Zero;
    private bool _handleFromLaunch;
    private Process? _process;

    public IntPtr DriverHandle { get; private set; } = IntPtr.Zero;

    public bool IsAttached => (_readHandle != IntPtr.Zero || (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1)) && _process != null && !_process.HasExited;
    public Process? Process => _process;

    /// <summary>Read-only handle — used for all ReadProcessMemory calls.</summary>
    public IntPtr Handle => _readHandle;

    /// <summary>Write handle — opened lazily on first write, caller must call CloseWriteHandle() after burst.</summary>
    public IntPtr WriteHandle
    {
        get
        {
            if (_handleFromLaunch)
                return _readHandle; // CreateProcess handle has PROCESS_ALL_ACCESS

            if (_writeHandle == IntPtr.Zero && _process != null && !_process.HasExited)
            {
                uint writeAccess = Native.PROCESS_VM_WRITE | Native.PROCESS_VM_OPERATION;
                _writeHandle = Native.OpenProcess(writeAccess, false, (uint)_process.Id);
            }
            return _writeHandle;
        }
    }

    public int ProcessId => _process?.Id ?? 0;
    public IntPtr BaseAddress => _process?.MainModule?.BaseAddress ?? IntPtr.Zero;
    public IntPtr WindowHandle { get; private set; } = IntPtr.Zero;
    public string? AttachedProcessName { get; private set; }
    public event Action? AttachStateChanged;
    public string ClientId => IsAttached ? $"{BaseAddress.ToInt64():X16}{ProcessId:X8}" : "";

    /// <summary>Full path to the attached process executable (resolved via Process.MainModule). Null if not attached.</summary>
    public string? ClientExePath
    {
        get
        {
            try { return _process?.MainModule?.FileName; }
            catch { return null; }
        }
    }

    /// <summary>Game window: attached process window, or first Muh.exe main window when not attached (for pixel mode + input).</summary>
    public IntPtr GetGameWindowOrNull()
    {
        if (IsAttached && WindowHandle != IntPtr.Zero) return WindowHandle;
        foreach (var p in System.Diagnostics.Process.GetProcessesByName("Muh"))
        {
            try
            {
                if (p.MainWindowHandle != IntPtr.Zero) return p.MainWindowHandle;
            }
            finally { p.Dispose(); }
        }
        return IntPtr.Zero;
    }

    /// <summary>Last attach failure: Win32 error and NtOpenProcess NTSTATUS (e.g. "Win32=5, NTSTATUS=0xC0000022"). Empty if attach succeeded or not yet attempted.</summary>
    public string LastAttachFailure { get; private set; } = "";

    /// <summary>Last injection failure: Win32 error code and step. Empty if injection succeeded or not yet attempted.</summary>
    public string LastInjectionFailure { get; private set; } = "";

    /// <summary>Close the write handle after a burst of writes. Reduces detection window.</summary>
    public void CloseWriteHandle()
    {
        if (_handleFromLaunch) return; // single handle with full access, don't close
        if (_writeHandle != IntPtr.Zero)
        {
            Native.CloseHandle(_writeHandle);
            _writeHandle = IntPtr.Zero;
        }
    }

    public static List<ProcessDisplayItem> EnumerateROProcesses()
    {
        var results = new List<ProcessDisplayItem>();
        var selfPid = System.Diagnostics.Process.GetCurrentProcess().Id;
        foreach (var name in CommonProcessNames)
        {
            foreach (var p in System.Diagnostics.Process.GetProcessesByName(name))
            {
                try
                {
                    if (p.Id == selfPid) continue;
                    if (p.MainWindowHandle == IntPtr.Zero) continue;

                    bool alreadyAttached;
                    lock (_pidLock) { alreadyAttached = _attachedPids.Contains(p.Id); }
                    string suffix = alreadyAttached ? " [ATTACHED]" : "";
                    results.Add(new ProcessDisplayItem(
                        p.ProcessName,
                        p.Id,
                        $"{p.ProcessName}.exe - PID {p.Id}{suffix}"));
                }
                catch { }
            }
        }
        return results.OrderBy(r => r.ProcessName).ToList();
    }

    public bool Attach()
    {
        Detach();
        foreach (var name in CommonProcessNames)
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(name);
            foreach (var p in processes)
            {
                lock (_pidLock)
                {
                    if (_attachedPids.Contains(p.Id)) continue;
                }
                if (AttachToProcess(p))
                    return true;
            }
        }
        return false;
    }

    public bool Attach(int processId)
    {
        Detach();
        try
        {
            var proc = System.Diagnostics.Process.GetProcessById(processId);
            return AttachToProcess(proc);
        }
        catch
        {
            Detach();
            return false;
        }
    }

    /// <summary>
    /// Launch the game executable and attach using the CreateProcess handle
    /// (obtained before Gepard Shield loads). Bypasses NtOpenProcess hooks.
    /// If earlyInjectDllPath is provided, injects that DLL immediately after process creation,
    /// before Gepard has a chance to load — critical for bypass to work.
    /// </summary>
    /// <summary>
    /// Launch the game executable. Note: Muh.exe can only be started via the patcher (account selection).
    /// Use Watch instead — it detects when the patcher starts Muh.exe and injects with suspend-first.
    /// </summary>
    public bool LaunchAndAttach(string exePath, string? earlyInjectDllPath = null)
    {
        Detach();
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = System.IO.Path.GetDirectoryName(exePath) ?? "",
                UseShellExecute = false
            };
            _process = Process.Start(startInfo);
            if (_process == null) return false;
            _readHandle = _process.Handle;
            _handleFromLaunch = true;

            if (!string.IsNullOrEmpty(earlyInjectDllPath) && System.IO.File.Exists(earlyInjectDllPath))
            {
                AttachViaShadowThreadLoader(_process.Id, earlyInjectDllPath!);
                System.Threading.Thread.Sleep(500);
            }

            // Wait for game window to appear (up to 30 seconds)
            for (int i = 0; i < 300; i++)
            {
                _process.Refresh();
                FindWindowHandle();
                if (WindowHandle != IntPtr.Zero) break;
                System.Threading.Thread.Sleep(100);
            }

            if (WindowHandle == IntPtr.Zero)
            {
                System.Diagnostics.Debug.WriteLine("[ProcessManager] LaunchAndAttach — window not found after 30s");
                Detach();
                return false;
            }

            AttachedProcessName = _process.ProcessName;
            lock (_pidLock) { _attachedPids.Add(_process.Id); }
            AttachStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] LaunchAndAttach exception: {ex.Message}");
            Detach();
            return false;
        }
    }

    /// <summary>
    /// Launch the MuhRO client (Muh.exe) from the default path and attach using
    /// the CreateProcess handle before Gepard Shield loads.
    /// </summary>
    public bool LaunchAndAttachMuhRO()
    {
        return LaunchAndAttach(MuhRoExePath);
    }

    public bool InjectDll(int processId, string dllPath)
    {
        return AttachViaShadowThreadLoader(processId, dllPath);
    }

    /// <summary>
    /// For Watch flow: suspend, inject ColdHide (optional) + HEAVENSGATE + Dll1 (before Gepard loads), connect pipe, then resume.
    /// ColdHide first (anti-anti-debug), then HEAVENSGATE (Gepard bypass), then Dll1 (packet pipe).
    /// </summary>
    public async Task<bool> AttachSuspendInjectBothConnectResumeAsync(int targetPid, string heavensGatePath, string dll1Path, Func<Task<bool>> connectPipeAsync, string? coldHidePath = null)
    {
        LastInjectionFailure = "";
        IntPtr hProcess = Native.OpenProcess(Native.PROCESS_ALL_ACCESS, false, (uint)targetPid);
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
                ColdHideConfigWriter.EnsureConfig(coldHidePath!);
                if (!InjectOneDll(hProcess, coldHidePath!)) return false;
                System.Threading.Thread.Sleep(300); // ColdHide: ~20 inline hooks + PEB + INI
            }
            if (!InjectOneDll(hProcess, heavensGatePath)) return false;
            System.Threading.Thread.Sleep(200); // HEAVENSGATE: FakeModule + CRC redirect setup
            if (!InjectOneDll(hProcess, dll1Path)) return false;
            System.Threading.Thread.Sleep(300); // Dll1: Detour attach + NamedPipeThread creation
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
        AttachedProcessName = _process.ProcessName;
        FindWindowHandle();
        lock (_pidLock) { _attachedPids.Add(targetPid); }
        AttachStateChanged?.Invoke();
        return true;
    }

    private bool InjectOneDll(IntPtr hProcess, string dllPath)
    {
        IntPtr pLoadLibrary = Native.GetProcAddress(Native.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (pLoadLibrary == IntPtr.Zero) { LastInjectionFailure = "GetProcAddress failed."; return false; }
        byte[] pathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
        IntPtr remoteMem = Native.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length, Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero) { LastInjectionFailure = "VirtualAllocEx failed."; return false; }
        if (!Native.WriteProcessMemory(hProcess, remoteMem, pathBytes, (uint)pathBytes.Length, out _))
        {
            Native.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
            LastInjectionFailure = "WriteProcessMemory failed.";
            return false;
        }
        IntPtr hThread = Native.CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, remoteMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            uint rt = Native.RtlCreateUserThread(hProcess, IntPtr.Zero, false, 0, IntPtr.Zero, IntPtr.Zero, pLoadLibrary, remoteMem, out hThread, out _);
            if (rt != 0 || hThread == IntPtr.Zero)
            {
                Native.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
                LastInjectionFailure = "CreateRemoteThread failed.";
                return false;
            }
        }
        Native.CloseHandle(hThread);
        return true;
    }

    /// <summary>
    /// For Watch flow: open process, suspend all threads (freeze before Gepard loads), inject DLL, resume.
    /// </summary>
    public bool AttachSuspendInjectResume(int targetPid, string dllPath)
    {
        LastInjectionFailure = "";
        IntPtr hProcess = Native.OpenProcess(Native.PROCESS_ALL_ACCESS, false, (uint)targetPid);
        if (hProcess == IntPtr.Zero)
        {
            LastInjectionFailure = $"OpenProcess failed (Win32={Marshal.GetLastWin32Error()}).";
            return false;
        }
        var threads = SuspendOrResumeAllThreads((uint)targetPid, true);
        try
        {
            if (!InjectOneDll(hProcess, dllPath)) return false;
            System.Threading.Thread.Sleep(50);
        }
        finally
        {
            SuspendOrResumeAllThreads((uint)targetPid, false, threads);
        }
        _readHandle = hProcess;
        _process = Process.GetProcessById(targetPid);
        _handleFromLaunch = false;
        AttachedProcessName = _process.ProcessName;
        FindWindowHandle();
        lock (_pidLock) { _attachedPids.Add(targetPid); }
        AttachStateChanged?.Invoke();
        return true;
    }

    private List<IntPtr> SuspendOrResumeAllThreads(uint processId, bool suspend, List<IntPtr>? existingHandles = null)
    {
        var handles = new List<IntPtr>();
        if (!suspend && existingHandles != null)
        {
            foreach (var h in existingHandles)
            {
                Native.ResumeThread(h);
                Native.CloseHandle(h);
            }
            return handles;
        }
        IntPtr snap = Native.CreateToolhelp32Snapshot(Native.TH32CS_SNAPTHREAD, 0);
        if (snap == IntPtr.Zero || snap.ToInt64() == -1) return handles;
        try
        {
            var te = new Native.THREADENTRY32 { dwSize = (uint)Marshal.SizeOf<Native.THREADENTRY32>() };
            if (!Native.Thread32First(snap, ref te)) return handles;
            do
            {
                if (te.th32OwnerProcessID != processId) continue;
                IntPtr hThread = Native.OpenThread(Native.THREAD_SUSPEND_RESUME, false, te.th32ThreadID);
                if (hThread == IntPtr.Zero) continue;
                Native.SuspendThread(hThread);
                handles.Add(hThread);
            } while (Native.Thread32Next(snap, ref te));
        }
        finally
        {
            Native.CloseHandle(snap);
        }
        return handles;
    }

    public bool InjectDllStealth(int processId, string dllPath)
    {
        // 1. We already have a hijacked handle if Attach() was called.
        // 2. We use that handle to perform the injection, which is much harder to detect.
        return AttachViaShadowThreadLoader(processId, dllPath);
    }

    public bool InjectDllKernel(int processId, string dllPath)
    {
        LastInjectionFailure = "";
        // 1. Find the driver. Check project-specific build folders first.
        string[] searchPaths = {
            System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kernel", "DeepVeil.sys"),
            @"E:\RAGNAROK ONLINE\Simple Ragnarok Program\DeepVeil\x64\Release\DeepVeil.sys",
            @"E:\RAGNAROK ONLINE\Simple Ragnarok Program\DeepVeil\x64\Release\DeepVeil\DeepVeil.sys"
        };

        string? sysPath = searchPaths.FirstOrDefault(System.IO.File.Exists);
        if (sysPath == null)
        {
            LastInjectionFailure = "DeepVeil.sys not found. Kernel inject requires WDK-built driver.";
            return false;
        }

        if (!StartDeepVeilDriver(sysPath))
        {
            LastInjectionFailure = "Could not load DeepVeil driver. Run as Administrator.";
            return false;
        }

        // 2. Ensure DLL path is absolute and accessible
        string fullDllPath = System.IO.Path.GetFullPath(dllPath);

        // 3. Resolve LoadLibraryW (simpler than LdrLoadDll for kernel thread start)
        IntPtr hKernel32 = Native.GetModuleHandle("kernel32.dll");
        IntPtr pLoadLibraryW = Native.GetProcAddress(hKernel32, "LoadLibraryW");

        var data = new Native.INJECT_DATA
        {
            TargetPid = (uint)processId,
            DllFullPath = fullDllPath,
            LdrLoadDllAddr = pLoadLibraryW
        };

        bool ok = Native.DeviceIoControl(DriverHandle, Native.IOCTL_KERNEL_INJECT, ref data, (uint)Marshal.SizeOf(data), null, 0, out _, IntPtr.Zero);
        if (!ok)
        {
            LastInjectionFailure = "Kernel inject failed (DeepVeil KernelInjectDll may be unimplemented). Use Launch instead.";
        }
        return ok;
    }

    private bool StartDeepVeilDriver(string driverPath)
    {
        DriverHandle = Native.CreateFile(@"\\.\DeepVeil", Native.GENERIC_READ | Native.GENERIC_WRITE, 0, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
        if (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1) return true;

        IntPtr scm = Native.OpenSCManager(null, null, Native.SC_MANAGER_ALL_ACCESS);
        if (scm == IntPtr.Zero) return false;

        string serviceName = "DeepVeil";
        IntPtr hService = Native.OpenService(scm, serviceName, Native.SERVICE_ALL_ACCESS);
        if (hService == IntPtr.Zero)
        {
            hService = Native.CreateService(scm, serviceName, "DeepVeil Kernel Injector", Native.SERVICE_ALL_ACCESS,
                Native.SERVICE_KERNEL_DRIVER, Native.SERVICE_DEMAND_START, Native.SERVICE_ERROR_NORMAL,
                driverPath, null, IntPtr.Zero, null, null, null);
        }

        bool started = false;
        if (hService != IntPtr.Zero)
        {
            started = Native.StartService(hService, 0, null);
            Native.CloseServiceHandle(hService);
        }
        Native.CloseServiceHandle(scm);

        DriverHandle = Native.CreateFile(@"\\.\DeepVeil", Native.GENERIC_READ | Native.GENERIC_WRITE, 0, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
        return DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1;
    }

    private bool AttachToProcess(Process proc)
    {
        try
        {
            _process = proc;
            uint readAccess = Native.PROCESS_VM_READ | Native.PROCESS_QUERY_INFORMATION;
            LastAttachFailure = "";
            _readHandle = Native.OpenProcess(readAccess, false, (uint)_process.Id);
            if (_readHandle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"[ProcessManager] OpenProcess failed for PID {_process.Id} — Win32 error {err}");
                
                // Fallback 1: NtOpenProcess via ntdll
                var clientId = new Native.CLIENT_ID
                {
                    UniqueProcess = (IntPtr)_process.Id,
                    UniqueThread = IntPtr.Zero
                };
                var objAttributes = new Native.OBJECT_ATTRIBUTES();
                objAttributes.Length = Marshal.SizeOf(typeof(Native.OBJECT_ATTRIBUTES));

                int nt = Native.NtOpenProcess(out var hProcess, readAccess, ref objAttributes, ref clientId);
                if (nt == Native.STATUS_SUCCESS && hProcess != IntPtr.Zero)
                {
                    _readHandle = hProcess;
                    System.Diagnostics.Debug.WriteLine($"[ProcessManager] NtOpenProcess (ntdll) succeeded for PID {_process.Id}");
                }
                else
                {
                    // Fallback 2: Handle Hijacking
                    System.Diagnostics.Debug.WriteLine($"[ProcessManager] NtOpenProcess failed. Attempting Handle Hijacking...");
                    if (!AttachViaHandleHijacking(proc.Id))
                    {
                        // Fallback 3: AbyssGate Kernel Driver
                        // AbyssGate provides memory read/write via MmCopyVirtualMemory, bypassing NtOpenProcess entirely.
                        // FsmEngine.dll (Lua hooking) and PacketInjector.dll (packet injection) are injected separately
                        // after attachment succeeds — they are action layers, not access providers.
                        System.Diagnostics.Debug.WriteLine($"[ProcessManager] Handle Hijacking failed. Attempting AbyssGate Kernel Driver connection...");
                        string sysPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kernel", "AbyssGate.sys");
                        if (System.IO.File.Exists(sysPath) && StartAbyssGateDriver(sysPath))
                        {
                            System.Diagnostics.Debug.WriteLine($"[ProcessManager] AbyssGate driver fully connected and ready to read memory for PID {_process.Id}");
                            return true;
                        }

                        LastAttachFailure = $"Win32={err}, NTSTATUS=0x{nt:X8}";
                        if (nt == unchecked((int)0xC0000022)) LastAttachFailure += " (ACCESS_DENIED)";
                        System.Diagnostics.Debug.WriteLine($"[ProcessManager] All fallbacks failed for PID {_process.Id}");
                        _process = null;
                        return false;                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessManager] Handle Hijacking succeeded for PID {_process.Id}");
                    }
                }
            }
            AttachedProcessName = _process.ProcessName;
            FindWindowHandle();
            lock (_pidLock) { _attachedPids.Add(_process.Id); }
            AttachStateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            LastAttachFailure = $"Exception: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] AttachToProcess exception: {ex.Message}");
            Detach();
            return false;
        }
    }

    /// <summary>
    /// Attempts to find an existing handle opened by another process (via NtQuerySystemInformation
    /// with SystemExtendedHandleInformation, class 64) and duplicate it into the bot.
    /// Uses pointer-width PIDs so processes with PID > 65535 are handled correctly.
    /// </summary>
    private bool AttachViaHandleHijacking(int targetPid)
    {
        int currentLength = 0x100000; // start at 1 MB — extended info is larger
        IntPtr ptr = Marshal.AllocHGlobal(currentLength);
        try
        {
            int status;
            while ((status = Native.NtQuerySystemInformation(Native.SystemExtendedHandleInformation, ptr, currentLength, out _)) == Native.STATUS_INFO_LENGTH_MISMATCH)
            {
                Marshal.FreeHGlobal(ptr);
                currentLength *= 2;
                ptr = Marshal.AllocHGlobal(currentLength);
            }

            if (status != Native.STATUS_SUCCESS)
            {
                System.Diagnostics.Debug.WriteLine($"[HandleHijack] NtQuerySystemInformation failed: 0x{status:X8}");
                return false;
            }

            // Extended info header: NumberOfHandles (IntPtr-width), Reserved (IntPtr-width)
            int ptrSize = IntPtr.Size; // 4 on x86, 8 on x64
            long handleCount = ptrSize == 8 ? Marshal.ReadInt64(ptr) : Marshal.ReadInt32(ptr);
            IntPtr handlePtr = IntPtr.Add(ptr, ptrSize * 2); // skip NumberOfHandles + Reserved
            int structSize = Marshal.SizeOf(typeof(Native.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX));

            IntPtr currentProcess = Native.GetCurrentProcess();
            uint myPid = Native.GetProcessId(currentProcess);

            System.Diagnostics.Debug.WriteLine($"[HandleHijack] Scanning {handleCount} handles for target PID {targetPid} (struct size={structSize}, ptrSize={ptrSize})");

            for (long i = 0; i < handleCount; i++)
            {
                var entry = Marshal.PtrToStructure<Native.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(handlePtr);
                handlePtr = IntPtr.Add(handlePtr, structSize);

                uint sourcePid = (uint)entry.UniqueProcessId.ToUInt64();

                // Skip handles owned by us or by the target itself (Gepard may block DuplicateHandle on self-handles)
                if (sourcePid == myPid || sourcePid == (uint)targetPid || sourcePid == 0 || sourcePid == 4)
                    continue;

                // We need a handle with at least PROCESS_VM_READ access
                uint access = entry.GrantedAccess;
                if ((access & Native.PROCESS_VM_READ) == 0 && access != Native.PROCESS_ALL_ACCESS && access != 0x1FFFFF)
                    continue;

                // Filter out common non-process granted access patterns to reduce noise
                // Process handles typically have access like 0x1FFFFF (PROCESS_ALL_ACCESS), 0x1F0FFF, 0x0410, 0x0430, etc.
                // Skip obvious non-process masks (e.g. file/key/section handles)
                if (access == 0x00100000 || access == 0x001F0001 || access == 0x0012019F || access == 0x00120089)
                    continue;

                IntPtr sourceProc = Native.OpenProcess(Native.PROCESS_DUP_HANDLE, false, sourcePid);
                if (sourceProc == IntPtr.Zero)
                    continue;

                IntPtr handleValue = (IntPtr)(long)entry.HandleValue.ToUInt64();

                // Attempt to duplicate handle into our process
                if (Native.DuplicateHandle(sourceProc, handleValue, currentProcess, out IntPtr dupHandle, 0, false, Native.DUPLICATE_SAME_ACCESS))
                {
                    // Verify if this handle actually points to the target process
                    try
                    {
                        uint dupTargetPid = Native.GetProcessId(dupHandle);
                        if (dupTargetPid == (uint)targetPid)
                        {
                            // Verify it's usable for ReadProcessMemory
                            byte[] testBuf = new byte[1];
                            bool canRead = Native.ReadProcessMemory(dupHandle, IntPtr.Zero, testBuf, 1, out _);
                            // Even if ReadProcessMemory fails at address 0, that's expected — 
                            // the important thing is we have a valid handle. GetProcessId succeeded = valid.

                            _readHandle = dupHandle;
                            Native.CloseHandle(sourceProc);

                            if (access == Native.PROCESS_ALL_ACCESS || access == 0x1FFFFF ||
                                (access & Native.PROCESS_VM_WRITE) == Native.PROCESS_VM_WRITE)
                            {
                                _handleFromLaunch = true;
                            }

                            System.Diagnostics.Debug.WriteLine($"[HandleHijack] SUCCESS! Hijacked handle 0x{handleValue:X} from PID {sourcePid} with access 0x{access:X}");
                            return true;
                        }
                    }
                    catch { /* GetProcessId can throw if handle isn't a process handle */ }

                    Native.CloseHandle(dupHandle);
                }
                Native.CloseHandle(sourceProc);
            }

            System.Diagnostics.Debug.WriteLine($"[HandleHijack] No suitable handle found after scanning {handleCount} entries");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HandleHijack] Exception: {ex.Message}");
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return false;
    }

    /// <summary>
    /// Fallback 3: ShadowThreadLoader. Injects a DLL into the process by creating a remote thread 
    /// mapped to LoadLibraryA. Requires a handle with VM_WRITE, VM_OPERATION, CREATE_THREAD.
    /// Prefer Launch+Attach to get handle before Gepard loads; otherwise try OpenProcess(PROCESS_ALL_ACCESS).
    /// </summary>
    private bool AttachViaShadowThreadLoader(int targetPid, string dllPath)
    {
        LastInjectionFailure = "";
        // Prefer full-access handle: from Launch we have it; otherwise try OpenProcess (may be blocked by Gepard).
        bool useReadHandle = _readHandle != IntPtr.Zero && _handleFromLaunch;
        IntPtr hProcess = useReadHandle ? _readHandle : Native.OpenProcess(Native.PROCESS_ALL_ACCESS, false, (uint)targetPid);
        if (hProcess == IntPtr.Zero && _readHandle != IntPtr.Zero)
        {
            hProcess = _readHandle; // Last resort: hijacked handle might have write access
        }
        if (hProcess == IntPtr.Zero)
        {
            LastInjectionFailure = $"OpenProcess failed (Win32={Marshal.GetLastWin32Error()}). Use Launch to attach before Gepard loads.";
            return false;
        }

        IntPtr pLoadLibrary = Native.GetProcAddress(Native.GetModuleHandle("kernel32.dll"), "LoadLibraryA");
        if (pLoadLibrary == IntPtr.Zero)
        {
            if (!useReadHandle && hProcess != _readHandle) Native.CloseHandle(hProcess);
            LastInjectionFailure = "GetProcAddress(LoadLibraryA) failed.";
            return false;
        }

        byte[] pathBytes = System.Text.Encoding.ASCII.GetBytes(dllPath + "\0");
        IntPtr remoteMem = Native.VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length, Native.MEM_COMMIT | Native.MEM_RESERVE, Native.PAGE_READWRITE);
        if (remoteMem == IntPtr.Zero)
        {
            if (!useReadHandle && hProcess != _readHandle) Native.CloseHandle(hProcess);
            LastInjectionFailure = $"VirtualAllocEx failed (Win32={Marshal.GetLastWin32Error()}). Need PROCESS_VM_OPERATION.";
            return false;
        }

        bool writeOk = Native.WriteProcessMemory(hProcess, remoteMem, pathBytes, (uint)pathBytes.Length, out _);
        if (!writeOk)
        {
            Native.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
            if (!useReadHandle && hProcess != _readHandle) Native.CloseHandle(hProcess);
            LastInjectionFailure = $"WriteProcessMemory failed (Win32={Marshal.GetLastWin32Error()}). Need PROCESS_VM_WRITE.";
            return false;
        }

        IntPtr hThread = Native.CreateRemoteThread(hProcess, IntPtr.Zero, 0, pLoadLibrary, remoteMem, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero)
        {
            // Fallback: RtlCreateUserThread — some anticheats hook CreateRemoteThread but not this
            uint rt = Native.RtlCreateUserThread(hProcess, IntPtr.Zero, false, 0, IntPtr.Zero, IntPtr.Zero, pLoadLibrary, remoteMem, out hThread, out _);
            if (rt != 0 || hThread == IntPtr.Zero)
            {
                Native.VirtualFreeEx(hProcess, remoteMem, 0, 0x8000);
                if (!useReadHandle && hProcess != _readHandle) Native.CloseHandle(hProcess);
                LastInjectionFailure = $"CreateRemoteThread and RtlCreateUserThread failed (Win32={Marshal.GetLastWin32Error()}). Try copying DLL to game folder.";
                return false;
            }
        }

        Native.CloseHandle(hThread);
        if (!useReadHandle && hProcess != _readHandle) Native.CloseHandle(hProcess);
        return true;
    }

    /// <summary>
    /// Kernel driver loader. Registers and starts the memory driver via SCM.
    /// Device name "WdBroker" blends with Windows Defender services to avoid
    /// detection by Gepard's QueryServiceStatusEx scan.
    /// </summary>
    private bool StartAbyssGateDriver(string driverPath)
    {
        DriverHandle = Native.CreateFile(@"\\.\WdBroker", Native.GENERIC_READ | Native.GENERIC_WRITE, 0, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
        if (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1) return true;

        IntPtr scm = Native.OpenSCManager(null, null, Native.SC_MANAGER_ALL_ACCESS);
        if (scm == IntPtr.Zero) return false;

        string serviceName = "WdBroker";
        IntPtr hService = Native.OpenService(scm, serviceName, Native.SERVICE_ALL_ACCESS);
        if (hService == IntPtr.Zero)
        {
            hService = Native.CreateService(scm, serviceName, "Windows Defender Broker Service", Native.SERVICE_ALL_ACCESS,
                Native.SERVICE_KERNEL_DRIVER, Native.SERVICE_DEMAND_START, Native.SERVICE_ERROR_NORMAL,
                driverPath, null, IntPtr.Zero, null, null, null);
        }

        bool started = false;
        if (hService != IntPtr.Zero)
        {
            started = Native.StartService(hService, 0, null);
            Native.CloseServiceHandle(hService);
        }
        Native.CloseServiceHandle(scm);

        DriverHandle = Native.CreateFile(@"\\.\WdBroker", Native.GENERIC_READ | Native.GENERIC_WRITE, 0, IntPtr.Zero, Native.OPEN_EXISTING, 0, IntPtr.Zero);
        return DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1;
    }

    /// <summary>
    /// Deploy the proxy DLL (winmm.dll) to the game folder. The game will load it
    /// automatically via DLL search order — no injection needed. This is the same
    /// technique Gepard's own dinput.dll uses to latch onto the client.
    ///
    /// The proxy:
    ///   1. Forwards all winmm.dll calls to the real System32\winmm.dll
    ///   2. Hooks Winsock (send/WSASend) for packet injection — MuhBotPacketPipe
    ///   3. Hooks the game's Lua engine for script execution — MuhBotLuaPipe
    ///   4. Routes Lua memory reads to C# via MuhBotMemoryPipe → AbyssGate/RPM
    ///
    /// Call this BEFORE the game launches. When the game starts, it loads our
    /// winmm.dll from its own folder instead of System32.
    /// </summary>
    /// <param name="gameFolder">Path to the game folder (where Muh.exe lives).</param>
    /// <returns>True if proxy was deployed successfully.</returns>
    public bool DeployProxyDll(string? gameFolder = null)
    {
        gameFolder ??= MuhRoGameFolder;
        string proxySource = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Injection", "winmm.dll");
        string proxyTarget = System.IO.Path.Combine(gameFolder, "winmm.dll");

        if (!System.IO.File.Exists(proxySource))
        {
            LastInjectionFailure = "Proxy DLL not found. Build ProxyLoader.cpp as winmm.dll first.";
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] Proxy DLL not found at: {proxySource}");
            return false;
        }

        try
        {
            // Back up existing winmm.dll if present (could be a real one or old proxy)
            if (System.IO.File.Exists(proxyTarget))
            {
                string backup = proxyTarget + ".bak";
                if (!System.IO.File.Exists(backup))
                    System.IO.File.Copy(proxyTarget, backup, false);
            }

            System.IO.File.Copy(proxySource, proxyTarget, true);
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] Proxy DLL deployed to: {proxyTarget}");
            return true;
        }
        catch (Exception ex)
        {
            LastInjectionFailure = $"Failed to deploy proxy DLL: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] Proxy deploy failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Remove the proxy DLL from the game folder and restore any backup.
    /// </summary>
    public bool RemoveProxyDll(string? gameFolder = null)
    {
        gameFolder ??= MuhRoGameFolder;
        string proxyTarget = System.IO.Path.Combine(gameFolder, "winmm.dll");
        string backup = proxyTarget + ".bak";

        try
        {
            if (System.IO.File.Exists(proxyTarget))
                System.IO.File.Delete(proxyTarget);

            if (System.IO.File.Exists(backup))
                System.IO.File.Move(backup, proxyTarget);

            System.Diagnostics.Debug.WriteLine("[ProcessManager] Proxy DLL removed");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProcessManager] Proxy removal failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Full proxy-based attach flow:
    ///   1. Deploy proxy DLL to game folder
    ///   2. Wait for the game process to appear
    ///   3. Attach to the process (for memory reading via AbyssGate/RPM)
    ///   4. Connect to the proxy's named pipes (packet + Lua)
    ///
    /// The proxy DLL handles Winsock hooking and Lua hooking internally —
    /// no CreateRemoteThread, no LoadLibrary injection, nothing for Gepard to detect.
    /// </summary>
    public async Task<bool> AttachViaProxyAsync(Func<Task<bool>> connectPipeAsync, string? gameFolder = null, int timeoutSeconds = 60)
    {
        gameFolder ??= MuhRoGameFolder;

        // Step 1: Deploy proxy
        if (!DeployProxyDll(gameFolder))
            return false;

        // Step 2: Wait for game process
        System.Diagnostics.Debug.WriteLine("[ProcessManager] Proxy deployed. Waiting for game to launch...");
        Process? gameProc = null;
        for (int i = 0; i < timeoutSeconds * 2; i++)
        {
            foreach (var name in CommonProcessNames)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    lock (_pidLock)
                    {
                        if (_attachedPids.Contains(p.Id)) continue;
                    }
                    gameProc = p;
                    break;
                }
                if (gameProc != null) break;
            }
            if (gameProc != null) break;
            await Task.Delay(500);
        }

        if (gameProc == null)
        {
            LastAttachFailure = "Game process did not appear within timeout.";
            return false;
        }

        _process = gameProc;
        System.Diagnostics.Debug.WriteLine($"[ProcessManager] Game process found: PID {_process.Id}");

        // Step 3: Get a memory reading handle (try all methods)
        uint readAccess = Native.PROCESS_VM_READ | Native.PROCESS_QUERY_INFORMATION;
        _readHandle = Native.OpenProcess(readAccess, false, (uint)_process.Id);
        if (_readHandle == IntPtr.Zero)
        {
            // NtOpenProcess fallback
            var clientId = new Native.CLIENT_ID { UniqueProcess = (IntPtr)_process.Id, UniqueThread = IntPtr.Zero };
            var objAttributes = new Native.OBJECT_ATTRIBUTES();
            objAttributes.Length = Marshal.SizeOf(typeof(Native.OBJECT_ATTRIBUTES));
            int nt = Native.NtOpenProcess(out var hProcess, readAccess, ref objAttributes, ref clientId);
            if (nt == Native.STATUS_SUCCESS && hProcess != IntPtr.Zero)
                _readHandle = hProcess;
            else if (!AttachViaHandleHijacking(_process.Id))
            {
                // AbyssGate fallback for memory reading
                string sysPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kernel", "AbyssGate.sys");
                if (System.IO.File.Exists(sysPath))
                    StartAbyssGateDriver(sysPath);
            }
        }

        // Step 4: Connect to proxy's named pipes
        // Give the proxy time to initialize (it waits 2s after game load)
        await Task.Delay(3000);
        bool pipeOk = await connectPipeAsync();
        if (!pipeOk)
            System.Diagnostics.Debug.WriteLine("[ProcessManager] Pipe connect failed — proxy may still be initializing");

        AttachedProcessName = _process.ProcessName;
        FindWindowHandle();
        lock (_pidLock) { _attachedPids.Add(_process.Id); }
        AttachStateChanged?.Invoke();
        return true;
    }

    private void FindWindowHandle()
    {
        WindowHandle = IntPtr.Zero;
        Native.EnumWindows((hWnd, _) =>
        {
            Native.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == ProcessId)
            {
                WindowHandle = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
    }

    public void Detach()
    {
        if (_process != null)
        {
            lock (_pidLock) { _attachedPids.Remove(_process.Id); }
        }
        CloseWriteHandle();
        if (_readHandle != IntPtr.Zero && !_handleFromLaunch)
        {
            Native.CloseHandle(_readHandle);
            _readHandle = IntPtr.Zero;
        }
        if (DriverHandle != IntPtr.Zero && DriverHandle.ToInt64() != -1)
        {
            Native.CloseHandle(DriverHandle);
            DriverHandle = IntPtr.Zero;
        }
        _handleFromLaunch = false;
        _process = null;
        AttachedProcessName = null;
        WindowHandle = IntPtr.Zero;
        LastAttachFailure = "";
        AttachStateChanged?.Invoke();
    }
}
