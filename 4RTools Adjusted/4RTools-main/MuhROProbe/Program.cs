using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

ApplicationConfiguration.Initialize();
Application.Run(new MainForm());

// ============================================================================
// MainForm — GUI with multi-backend memory probing
// ============================================================================

internal sealed class MainForm : Form
{
    private readonly ComboBox _processCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _refreshButton = new() { Text = "Refresh" };
    private readonly Button _probeButton = new() { Text = "Probe" };
    private readonly Button _copyButton = new() { Text = "Copy Output" };
    private readonly TextBox _outputBox = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        ReadOnly = true,
        WordWrap = false
    };

    private readonly IReadOnlyList<CandidateAddress> _fallbackCandidates =
    [
        new("Muh", "MuhRO candidate A", 0x010DCE10, 0x010DF5D8),
        new("Muh", "MuhRO candidate B", 0x011D1A04, 0x011D43E8),
        new("Muh", "MuhRO candidate C", 0x00FFB858, 0x00FFDEF8),
        new("Muh", "MuhRO candidate D", 0x01138DDC, 0x0113B7B0),
        new("Muh", "MuhRO candidate E", 0x00F45E54, 0x00F48798),
        new("Muh", "MuhRO candidate F", 0x00E8F434, 0x00E91C00),
        new("Muh", "MuhRO candidate G", 0x01017F38, 0x0101A6E0),
    ];

    public MainForm()
    {
        Text = "MuhRO Probe";
        Width = 880;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;

        var instructionLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 70,
            Text = "Open MuhRO, log into a character, then click Probe. " +
                   "This tool tests HP/Name address pairs using multiple read methods (standard → direct ntdll → direct syscall).",
            Padding = new Padding(12, 12, 12, 0)
        };

        var topPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            ColumnCount = 4,
            Padding = new Padding(12, 0, 12, 0)
        };
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _refreshButton.Click += (_, _) => RefreshProcesses();
        _probeButton.Click += (_, _) => RunProbe();
        _copyButton.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(_outputBox.Text))
            {
                Clipboard.SetText(_outputBox.Text);
            }
        };

        topPanel.Controls.Add(_processCombo, 0, 0);
        topPanel.Controls.Add(_refreshButton, 1, 0);
        topPanel.Controls.Add(_probeButton, 2, 0);
        topPanel.Controls.Add(_copyButton, 3, 0);

        _outputBox.Dock = DockStyle.Fill;
        _outputBox.Font = new Font("Consolas", 10);

        Controls.Add(_outputBox);
        Controls.Add(topPanel);
        Controls.Add(instructionLabel);

        Shown += (_, _) => RefreshProcesses();
    }

    private void RefreshProcesses()
    {
        _processCombo.Items.Clear();
        foreach (var process in Process.GetProcessesByName("Muh").OrderBy(p => p.Id))
        {
            string title = string.IsNullOrWhiteSpace(process.MainWindowTitle) ? "(no window title)" : process.MainWindowTitle;
            _processCombo.Items.Add(new ProcessItem(process.Id, $"{process.ProcessName}.exe - {process.Id} - {title}"));
        }

        if (_processCombo.Items.Count > 0)
        {
            _processCombo.SelectedIndex = 0;
            AppendLine($"Found {_processCombo.Items.Count} Muh.exe process(es).");
        }
        else
        {
            AppendLine("No running Muh.exe process found.");
        }
    }

    private void RunProbe()
    {
        if (_processCombo.SelectedItem is not ProcessItem processItem)
        {
            AppendLine("Select a Muh.exe process first.");
            return;
        }

        _outputBox.Clear();
        AppendLine($"Probing PID {processItem.ProcessId}.");
        AppendLine("Running as Administrator — trying multiple memory read backends.");
        AppendLine(string.Empty);

        // Step 1: Enable SeDebugPrivilege
        bool debugPriv = PrivilegeHelper.EnableDebugPrivilege();
        AppendLine($"SeDebugPrivilege: {(debugPriv ? "ENABLED" : "FAILED (non-fatal)")}");
        AppendLine(string.Empty);

        // Step 2: Try each read backend in order
        IMemoryReader? workingReader = null;
        string workingMethod = "";

        var backends = new (string Name, Func<int, IMemoryReader?> Factory)[]
        {
            ("Standard API (ReadProcessMemory)", pid => StandardReader.TryCreate(pid)),
            ("Standard API + ALL_ACCESS",        pid => StandardReader.TryCreateFullAccess(pid)),
            ("Direct ntdll (NtReadVirtualMemory)", pid => DirectNtdllReader.TryCreate(pid)),
            ("Direct Syscall (raw syscall stub)",  pid => SyscallReader.TryCreate(pid)),
            ("Duplicate Handle",                   pid => DuplicateHandleReader.TryCreate(pid)),
        };

        foreach (var (name, factory) in backends)
        {
            AppendLine($"Trying: {name}...");
            try
            {
                var reader = factory(processItem.ProcessId);
                if (reader != null)
                {
                    // Quick sanity test: try to read 4 bytes from first candidate
                    byte[] test = new byte[4];
                    if (reader.ReadMemory((IntPtr)_fallbackCandidates[0].HpAddress, test, out int bytesRead) && bytesRead == 4)
                    {
                        workingReader = reader;
                        workingMethod = name;
                        AppendLine($"  → SUCCESS (read {bytesRead} bytes)");
                        break;
                    }
                    else
                    {
                        int err = Marshal.GetLastWin32Error();
                        AppendLine($"  → Handle opened but ReadMemory failed (error {err})");
                        reader.Dispose();
                    }
                }
                else
                {
                    int err = Marshal.GetLastWin32Error();
                    AppendLine($"  → Failed to open process (error {err})");
                }
            }
            catch (Exception ex)
            {
                AppendLine($"  → Exception: {ex.Message}");
            }
        }

        AppendLine(string.Empty);

        if (workingReader == null)
        {
            AppendLine("ALL memory read methods failed.");
            AppendLine("Gepard Shield is blocking external process memory access at kernel level.");
            AppendLine(string.Empty);
            AppendLine("Possible workarounds:");
            AppendLine("  1. Inject a DLL into the game process (bypass needed)");
            AppendLine("  2. Use a kernel driver to read memory (requires code signing)");
            AppendLine("  3. Use shared memory / IPC from an injected DLL");
            AppendLine("  4. Read game network packets instead of memory");
            SaveReport();
            return;
        }

        AppendLine($"Using: {workingMethod}");
        AppendLine(string.Empty);

        using (workingReader)
        {
            var candidates = LoadCandidates();
            AppendLine($"Testing {candidates.Count} candidate address pair(s).");
            AppendLine(string.Empty);

            var matches = new List<ProbeResult>();
            foreach (var candidate in candidates)
            {
                if (TryProbeCandidate(workingReader, candidate, out var result))
                {
                    matches.Add(result);
                }
            }

            matches = matches
                .OrderByDescending(m => m.Score)
                .ThenBy(m => m.Candidate.Description, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (matches.Count == 0)
            {
                AppendLine("No candidate looked valid.");
                AppendLine("Memory was readable but addresses didn't match expected data layout.");
                AppendLine("Send me this output and I will widen the scan.");
                SaveReport();
                return;
            }

            AppendLine("Likely matches:");
            AppendLine(string.Empty);

            foreach (var match in matches.Take(10))
            {
                AppendLine($"{match.Candidate.Description}");
                AppendLine($"Process Name : {match.Candidate.Name}");
                AppendLine($"HP Address   : {match.Candidate.HpAddressHex}");
                AppendLine($"Name Address : {match.Candidate.NameAddressHex}");
                AppendLine($"Name         : {match.CharacterName}");
                AppendLine($"HP/SP        : {match.CurrentHp}/{match.MaxHp} HP, {match.CurrentSp}/{match.MaxSp} SP");
                AppendLine($"Score        : {match.Score}");
                AppendLine(string.Empty);
            }

            var best = matches[0];
            AppendLine("Suggested 4RTools entry:");
            AppendLine($"Process Name = {best.Candidate.Name}");
            AppendLine($"HP Address   = {best.Candidate.HpAddressHex[2..]}");
            AppendLine($"Name Address = {best.Candidate.NameAddressHex[2..]}");
        }

        SaveReport();
    }

    private List<CandidateAddress> LoadCandidates()
    {
        var candidates = new List<CandidateAddress>(_fallbackCandidates);
        foreach (string fileName in new[] { "custom_supported_servers.json", "supported_servers.json" })
        {
            string? path = FindFileInAncestors(fileName);
            if (path is null) continue;

            try
            {
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<CandidateJson>>(json) ?? [];
                foreach (var item in loaded)
                {
                    if (string.IsNullOrWhiteSpace(item.Name) ||
                        string.IsNullOrWhiteSpace(item.HpAddress) ||
                        string.IsNullOrWhiteSpace(item.NameAddress))
                        continue;

                    if (!TryParseHex(item.HpAddress, out int hpAddress) ||
                        !TryParseHex(item.NameAddress, out int nameAddress))
                        continue;

                    candidates.Add(new CandidateAddress(
                        item.Name.Trim(),
                        string.IsNullOrWhiteSpace(item.Description) ? item.Name.Trim() : item.Description.Trim(),
                        hpAddress,
                        nameAddress));
                }
            }
            catch { }
        }

        return candidates
            .GroupBy(c => $"{c.Name}|{c.HpAddressHex}|{c.NameAddressHex}", StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static string? FindFileInAncestors(string fileName)
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string candidatePath = Path.Combine(current.FullName, fileName);
            if (File.Exists(candidatePath)) return candidatePath;
            current = current.Parent;
        }
        return null;
    }

    private static bool TryParseHex(string value, out int parsed)
    {
        string normalized = value.Trim();
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..];
        return int.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, null, out parsed);
    }

    private static bool TryProbeCandidate(IMemoryReader reader, CandidateAddress candidate, out ProbeResult result)
    {
        result = default!;

        if (!TryReadUInt32(reader, candidate.HpAddress, out uint currentHp) ||
            !TryReadUInt32(reader, candidate.HpAddress + 4, out uint maxHp) ||
            !TryReadUInt32(reader, candidate.HpAddress + 8, out uint currentSp) ||
            !TryReadUInt32(reader, candidate.HpAddress + 12, out uint maxSp) ||
            !TryReadAscii(reader, candidate.NameAddress, 32, out string characterName))
        {
            return false;
        }

        bool statsLookValid =
            maxHp > 1 &&
            maxHp < 10_000_000 &&
            currentHp <= maxHp &&
            maxSp < 10_000_000 &&
            currentSp <= maxSp;

        bool nameLooksValid =
            characterName.Length >= 2 &&
            characterName.Length <= 24 &&
            characterName.All(ch => !char.IsControl(ch));

        int score = 0;
        if (statsLookValid) score += 2;
        if (nameLooksValid) score += 3;
        if (characterName.Any(char.IsLetter)) score += 1;

        if (score == 0) return false;

        result = new ProbeResult(candidate, currentHp, maxHp, currentSp, maxSp, characterName, score);
        return true;
    }

    private static bool TryReadUInt32(IMemoryReader reader, int address, out uint value)
    {
        value = 0;
        byte[] buffer = new byte[4];
        if (!reader.ReadMemory((IntPtr)address, buffer, out int bytesRead) || bytesRead != 4)
            return false;
        value = BitConverter.ToUInt32(buffer, 0);
        return true;
    }

    private static bool TryReadAscii(IMemoryReader reader, int address, int bufferSize, out string value)
    {
        value = string.Empty;
        byte[] buffer = new byte[bufferSize];
        if (!reader.ReadMemory((IntPtr)address, buffer, out int bytesRead) || bytesRead <= 0)
            return false;
        int length = Array.IndexOf(buffer, (byte)0);
        if (length < 0) length = bytesRead;
        value = Encoding.Default.GetString(buffer, 0, length).Trim();
        return true;
    }

    private void SaveReport()
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "MuhROProbe-results.txt");
            File.WriteAllText(path, _outputBox.Text);
            AppendLine(string.Empty);
            AppendLine($"Saved report to: {path}");
        }
        catch (Exception ex)
        {
            AppendLine(string.Empty);
            AppendLine($"Could not save report: {ex.Message}");
        }
    }

    private void AppendLine(string text)
    {
        if (_outputBox.TextLength > 0)
            _outputBox.AppendText(Environment.NewLine);
        _outputBox.AppendText(text);
    }
}

// ============================================================================
// Records
// ============================================================================

internal sealed record ProcessItem(int ProcessId, string DisplayText)
{
    public override string ToString() => DisplayText;
}

internal sealed record CandidateAddress(string Name, string Description, int HpAddress, int NameAddress)
{
    public string HpAddressHex => $"0x{HpAddress:X8}";
    public string NameAddressHex => $"0x{NameAddress:X8}";
}

internal sealed record ProbeResult(
    CandidateAddress Candidate,
    uint CurrentHp, uint MaxHp,
    uint CurrentSp, uint MaxSp,
    string CharacterName, int Score);

internal sealed class CandidateJson
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("hpAddress")] public string? HpAddress { get; set; }
    [JsonPropertyName("nameAddress")] public string? NameAddress { get; set; }
}

// ============================================================================
// IMemoryReader — abstract interface for memory reading backends
// ============================================================================

internal interface IMemoryReader : IDisposable
{
    bool ReadMemory(IntPtr address, byte[] buffer, out int bytesRead);
}

// ============================================================================
// SeDebugPrivilege enabler
// ============================================================================

internal static class PrivilegeHelper
{
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValueW(string? lpSystemName, string lpName, out long lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public int PrivilegeCount;
        public long Luid;
        public int Attributes;
    }

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const int SE_PRIVILEGE_ENABLED = 0x00000002;

    public static bool EnableDebugPrivilege()
    {
        try
        {
            if (!OpenProcessToken(Process.GetCurrentProcess().Handle,
                TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out IntPtr tokenHandle))
                return false;

            try
            {
                if (!LookupPrivilegeValueW(null, "SeDebugPrivilege", out long luid))
                    return false;

                var tp = new TOKEN_PRIVILEGES
                {
                    PrivilegeCount = 1,
                    Luid = luid,
                    Attributes = SE_PRIVILEGE_ENABLED
                };

                if (!AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
                    return false;

                return Marshal.GetLastWin32Error() == 0;
            }
            finally
            {
                CloseHandle(tokenHandle);
            }
        }
        catch
        {
            return false;
        }
    }
}

// ============================================================================
// Backend 1: Standard ReadProcessMemory
// ============================================================================

internal sealed class StandardReader : IMemoryReader
{
    private readonly IntPtr _handle;

    private StandardReader(IntPtr handle) => _handle = handle;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_ALL_ACCESS = 0x001FFFFF;

    public static StandardReader? TryCreate(int pid)
    {
        IntPtr h = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);
        if (h == IntPtr.Zero || h == new IntPtr(-1)) return null;
        return new StandardReader(h);
    }

    public static StandardReader? TryCreateFullAccess(int pid)
    {
        IntPtr h = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
        if (h == IntPtr.Zero || h == new IntPtr(-1)) return null;
        return new StandardReader(h);
    }

    public bool ReadMemory(IntPtr address, byte[] buffer, out int bytesRead)
    {
        bytesRead = 0;
        if (!ReadProcessMemory(_handle, address, buffer, buffer.Length, out IntPtr br))
            return false;
        bytesRead = br.ToInt32();
        return true;
    }

    public void Dispose() => CloseHandle(_handle);
}

// ============================================================================
// Backend 2: Direct ntdll function calls (bypasses IAT hooks)
// ============================================================================

internal sealed class DirectNtdllReader : IMemoryReader
{
    private readonly IntPtr _handle;
    private readonly NtReadDelegate _ntRead;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtOpenProcessDelegate(out IntPtr ProcessHandle, uint DesiredAccess,
        ref OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtReadDelegate(IntPtr ProcessHandle, IntPtr BaseAddress,
        byte[] Buffer, int NumberOfBytesToRead, out IntPtr NumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandleA(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    private DirectNtdllReader(IntPtr handle, NtReadDelegate ntRead)
    {
        _handle = handle;
        _ntRead = ntRead;
    }

    public static DirectNtdllReader? TryCreate(int pid)
    {
        IntPtr ntdll = GetModuleHandleA("ntdll.dll");
        if (ntdll == IntPtr.Zero) return null;

        IntPtr pNtOpenProcess = GetProcAddress(ntdll, "NtOpenProcess");
        IntPtr pNtReadVirtualMemory = GetProcAddress(ntdll, "NtReadVirtualMemory");
        if (pNtOpenProcess == IntPtr.Zero || pNtReadVirtualMemory == IntPtr.Zero) return null;

        var ntOpen = Marshal.GetDelegateForFunctionPointer<NtOpenProcessDelegate>(pNtOpenProcess);
        var ntRead = Marshal.GetDelegateForFunctionPointer<NtReadDelegate>(pNtReadVirtualMemory);

        var oa = new OBJECT_ATTRIBUTES();
        var cid = new CLIENT_ID { UniqueProcess = (IntPtr)pid };

        int status = ntOpen(out IntPtr handle, PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, ref oa, ref cid);
        if (status != 0 || handle == IntPtr.Zero)
            return null;

        return new DirectNtdllReader(handle, ntRead);
    }

    public bool ReadMemory(IntPtr address, byte[] buffer, out int bytesRead)
    {
        bytesRead = 0;
        int status = _ntRead(_handle, address, buffer, buffer.Length, out IntPtr br);
        if (status != 0) return false;
        bytesRead = br.ToInt32();
        return true;
    }

    public void Dispose() => CloseHandle(_handle);
}

// ============================================================================
// Backend 3: Direct Syscalls (raw syscall instruction, bypasses ALL user-mode hooks)
// ============================================================================

internal sealed class SyscallReader : IMemoryReader
{
    private readonly IntPtr _handle;
    private readonly NtReadVirtualMemoryDelegate _ntReadSyscall;
    private readonly IntPtr _execMemory;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtOpenProcessSyscallDelegate(out IntPtr ProcessHandle, uint DesiredAccess,
        ref OBJECT_ATTRIBUTES ObjectAttributes, ref CLIENT_ID ClientId);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtReadVirtualMemoryDelegate(IntPtr ProcessHandle, IntPtr BaseAddress,
        byte[] Buffer, int NumberOfBytesToRead, out IntPtr NumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize,
        uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandleA(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;

    private SyscallReader(IntPtr handle, NtReadVirtualMemoryDelegate ntReadSyscall, IntPtr execMemory)
    {
        _handle = handle;
        _ntReadSyscall = ntReadSyscall;
        _execMemory = execMemory;
    }

    /// <summary>
    /// Resolves the Syscall Service Number (SSN) for a given ntdll function.
    /// On x64 Windows, ntdll stubs follow the pattern:
    ///   4C 8B D1          mov r10, rcx
    ///   B8 xx xx xx xx    mov eax, SSN
    ///   ...
    ///   0F 05             syscall
    /// The SSN is at offset +4 from the function start.
    /// If Gepard has hooked the function (0xE9 JMP at start), we fall back to
    /// reading a clean copy of ntdll from disk.
    /// </summary>
    private static bool TryGetSsn(string functionName, out int ssn)
    {
        ssn = -1;
        IntPtr ntdll = GetModuleHandleA("ntdll.dll");
        if (ntdll == IntPtr.Zero) return false;

        IntPtr funcAddr = GetProcAddress(ntdll, functionName);
        if (funcAddr == IntPtr.Zero) return false;

        // Check if function is hooked (starts with 0xE9 = JMP)
        byte firstByte = Marshal.ReadByte(funcAddr);
        if (firstByte == 0x4C)
        {
            // Standard ntdll stub: 4C 8B D1 B8 xx xx xx xx
            // SSN is at offset +4
            ssn = Marshal.ReadInt32(funcAddr + 4);
            return true;
        }

        // Function might be hooked — try reading clean ntdll from disk
        return TryGetSsnFromDisk(functionName, out ssn);
    }

    /// <summary>
    /// Reads a clean copy of ntdll.dll from disk to extract SSNs,
    /// bypassing any in-memory hooks.
    /// </summary>
    private static bool TryGetSsnFromDisk(string functionName, out int ssn)
    {
        ssn = -1;
        try
        {
            // ntdll.dll is always in System32 (even for WoW64 processes, we want the native one)
            string ntdllPath = Path.Combine(Environment.SystemDirectory, "ntdll.dll");
            byte[] fileBytes = File.ReadAllBytes(ntdllPath);

            // Parse PE headers to find export table
            int peOffset = BitConverter.ToInt32(fileBytes, 0x3C);
            // Check PE signature
            if (BitConverter.ToInt32(fileBytes, peOffset) != 0x00004550) return false; // "PE\0\0"

            int optionalHeaderOffset = peOffset + 4 + 20; // PE sig + COFF header
            // Export directory RVA is at offset 112 in optional header (x64) or 96 (x86)
            int exportDirRva;
            ushort magic = BitConverter.ToUInt16(fileBytes, optionalHeaderOffset);
            if (magic == 0x20B) // PE32+ (x64)
                exportDirRva = BitConverter.ToInt32(fileBytes, optionalHeaderOffset + 112);
            else // PE32 (x86)
                exportDirRva = BitConverter.ToInt32(fileBytes, optionalHeaderOffset + 96);

            if (exportDirRva == 0) return false;

            // Convert RVA to file offset using section headers
            int exportDirFileOffset = RvaToFileOffset(fileBytes, peOffset, exportDirRva);
            if (exportDirFileOffset < 0) return false;

            int numberOfFunctions = BitConverter.ToInt32(fileBytes, exportDirFileOffset + 20);
            int numberOfNames = BitConverter.ToInt32(fileBytes, exportDirFileOffset + 24);
            int addressOfFunctionsRva = BitConverter.ToInt32(fileBytes, exportDirFileOffset + 28);
            int addressOfNamesRva = BitConverter.ToInt32(fileBytes, exportDirFileOffset + 32);
            int addressOfOrdinalsRva = BitConverter.ToInt32(fileBytes, exportDirFileOffset + 36);

            int addressOfFunctions = RvaToFileOffset(fileBytes, peOffset, addressOfFunctionsRva);
            int addressOfNames = RvaToFileOffset(fileBytes, peOffset, addressOfNamesRva);
            int addressOfOrdinals = RvaToFileOffset(fileBytes, peOffset, addressOfOrdinalsRva);

            if (addressOfFunctions < 0 || addressOfNames < 0 || addressOfOrdinals < 0) return false;

            for (int i = 0; i < numberOfNames; i++)
            {
                int nameRva = BitConverter.ToInt32(fileBytes, addressOfNames + i * 4);
                int nameFileOffset = RvaToFileOffset(fileBytes, peOffset, nameRva);
                if (nameFileOffset < 0) continue;

                // Read null-terminated string
                int end = nameFileOffset;
                while (end < fileBytes.Length && fileBytes[end] != 0) end++;
                string name = Encoding.ASCII.GetString(fileBytes, nameFileOffset, end - nameFileOffset);

                if (name == functionName)
                {
                    ushort ordinal = BitConverter.ToUInt16(fileBytes, addressOfOrdinals + i * 2);
                    int funcRva = BitConverter.ToInt32(fileBytes, addressOfFunctions + ordinal * 4);
                    int funcFileOffset = RvaToFileOffset(fileBytes, peOffset, funcRva);
                    if (funcFileOffset < 0) return false;

                    // Verify it's a syscall stub: 4C 8B D1 B8 xx xx xx xx
                    if (funcFileOffset + 8 > fileBytes.Length) return false;
                    if (fileBytes[funcFileOffset] == 0x4C &&
                        fileBytes[funcFileOffset + 1] == 0x8B &&
                        fileBytes[funcFileOffset + 2] == 0xD1 &&
                        fileBytes[funcFileOffset + 3] == 0xB8)
                    {
                        ssn = BitConverter.ToInt32(fileBytes, funcFileOffset + 4);
                        return true;
                    }

                    return false;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static int RvaToFileOffset(byte[] pe, int peOffset, int rva)
    {
        int numberOfSections = BitConverter.ToInt16(pe, peOffset + 4 + 2);
        int sizeOfOptionalHeader = BitConverter.ToInt16(pe, peOffset + 4 + 16);
        int sectionHeadersOffset = peOffset + 4 + 20 + sizeOfOptionalHeader;

        for (int i = 0; i < numberOfSections; i++)
        {
            int sectionOffset = sectionHeadersOffset + i * 40;
            int virtualSize = BitConverter.ToInt32(pe, sectionOffset + 8);
            int virtualAddress = BitConverter.ToInt32(pe, sectionOffset + 12);
            int rawDataSize = BitConverter.ToInt32(pe, sectionOffset + 16);
            int rawDataPointer = BitConverter.ToInt32(pe, sectionOffset + 20);

            int sectionEnd = virtualAddress + Math.Max(virtualSize, rawDataSize);
            if (rva >= virtualAddress && rva < sectionEnd)
            {
                return rawDataPointer + (rva - virtualAddress);
            }
        }

        return -1;
    }

    /// <summary>
    /// Builds a raw syscall stub in executable memory:
    ///   4C 8B D1          mov r10, rcx
    ///   B8 xx xx xx xx    mov eax, SSN
    ///   0F 05             syscall
    ///   C3                ret
    /// Total: 12 bytes per stub
    /// </summary>
    private static IntPtr BuildSyscallStub(IntPtr baseAddr, int offset, int ssn)
    {
        IntPtr addr = baseAddr + offset;
        byte[] stub = new byte[]
        {
            0x4C, 0x8B, 0xD1,                                  // mov r10, rcx
            0xB8,                                               // mov eax, ...
            (byte)(ssn & 0xFF),
            (byte)((ssn >> 8) & 0xFF),
            (byte)((ssn >> 16) & 0xFF),
            (byte)((ssn >> 24) & 0xFF),
            0x0F, 0x05,                                         // syscall
            0xC3                                                // ret
        };
        Marshal.Copy(stub, 0, addr, stub.Length);
        return addr;
    }

    public static SyscallReader? TryCreate(int pid)
    {
        // Resolve SSNs
        if (!TryGetSsn("NtOpenProcess", out int ssnOpen)) return null;
        if (!TryGetSsn("NtReadVirtualMemory", out int ssnRead)) return null;

        // Allocate executable memory for two syscall stubs (12 bytes each, 64 bytes to be safe)
        IntPtr execMem = VirtualAlloc(IntPtr.Zero, (UIntPtr)64,
            MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        if (execMem == IntPtr.Zero) return null;

        // Build NtOpenProcess stub at offset 0
        IntPtr openStubAddr = BuildSyscallStub(execMem, 0, ssnOpen);
        // Build NtReadVirtualMemory stub at offset 16
        IntPtr readStubAddr = BuildSyscallStub(execMem, 16, ssnRead);

        var ntOpenSyscall = Marshal.GetDelegateForFunctionPointer<NtOpenProcessSyscallDelegate>(openStubAddr);
        var ntReadSyscall = Marshal.GetDelegateForFunctionPointer<NtReadVirtualMemoryDelegate>(readStubAddr);

        // Open process via direct syscall
        var oa = new OBJECT_ATTRIBUTES();
        var cid = new CLIENT_ID { UniqueProcess = (IntPtr)pid };

        int status = ntOpenSyscall(out IntPtr handle, PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, ref oa, ref cid);
        if (status != 0 || handle == IntPtr.Zero)
        {
            // Try with more access flags
            status = ntOpenSyscall(out handle, 0x001FFFFF, ref oa, ref cid); // PROCESS_ALL_ACCESS
            if (status != 0 || handle == IntPtr.Zero)
            {
                VirtualFree(execMem, UIntPtr.Zero, MEM_RELEASE);
                return null;
            }
        }

        return new SyscallReader(handle, ntReadSyscall, execMem);
    }

    public bool ReadMemory(IntPtr address, byte[] buffer, out int bytesRead)
    {
        bytesRead = 0;
        int status = _ntReadSyscall(_handle, address, buffer, buffer.Length, out IntPtr br);
        if (status != 0) return false;
        bytesRead = br.ToInt32();
        return true;
    }

    public void Dispose()
    {
        CloseHandle(_handle);
        if (_execMemory != IntPtr.Zero)
            VirtualFree(_execMemory, UIntPtr.Zero, MEM_RELEASE);
    }
}

// ============================================================================
// Backend 4: Duplicate Handle (try to find an existing handle to the process)
// ============================================================================

internal sealed class DuplicateHandleReader : IMemoryReader
{
    private readonly IntPtr _handle;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NtReadDelegate(IntPtr ProcessHandle, IntPtr BaseAddress,
        byte[] Buffer, int NumberOfBytesToRead, out IntPtr NumberOfBytesRead);

    private readonly NtReadDelegate _ntRead;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle,
        uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandleA(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    private const uint PROCESS_DUP_HANDLE = 0x0040;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;

    private DuplicateHandleReader(IntPtr handle, NtReadDelegate ntRead)
    {
        _handle = handle;
        _ntRead = ntRead;
    }

    public static DuplicateHandleReader? TryCreate(int pid)
    {
        // Try to open with DUP_HANDLE permission (sometimes allowed when VM_READ isn't)
        IntPtr sourceProcess = OpenProcess(PROCESS_DUP_HANDLE, false, pid);
        if (sourceProcess == IntPtr.Zero || sourceProcess == new IntPtr(-1))
            return null;

        // Try to open with minimal access then duplicate with more rights
        IntPtr minHandle = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
        if (minHandle == IntPtr.Zero || minHandle == new IntPtr(-1))
        {
            CloseHandle(sourceProcess);
            return null;
        }

        bool success = DuplicateHandle(
            GetCurrentProcess(), minHandle,
            GetCurrentProcess(), out IntPtr dupHandle,
            PROCESS_VM_READ | PROCESS_QUERY_INFORMATION,
            false, 0);

        CloseHandle(minHandle);
        CloseHandle(sourceProcess);

        if (!success || dupHandle == IntPtr.Zero)
            return null;

        // Get NtReadVirtualMemory from ntdll
        IntPtr ntdll = GetModuleHandleA("ntdll.dll");
        if (ntdll == IntPtr.Zero) { CloseHandle(dupHandle); return null; }
        IntPtr pNtRead = GetProcAddress(ntdll, "NtReadVirtualMemory");
        if (pNtRead == IntPtr.Zero) { CloseHandle(dupHandle); return null; }
        var ntRead = Marshal.GetDelegateForFunctionPointer<NtReadDelegate>(pNtRead);

        return new DuplicateHandleReader(dupHandle, ntRead);
    }

    public bool ReadMemory(IntPtr address, byte[] buffer, out int bytesRead)
    {
        bytesRead = 0;
        int status = _ntRead(_handle, address, buffer, buffer.Length, out IntPtr br);
        if (status != 0) return false;
        bytesRead = br.ToInt32();
        return true;
    }

    public void Dispose() => CloseHandle(_handle);
}

// ============================================================================
// NT Structures (shared by all backends)
// ============================================================================

[StructLayout(LayoutKind.Sequential)]
internal struct OBJECT_ATTRIBUTES
{
    public int Length;
    public IntPtr RootDirectory;
    public IntPtr ObjectName;
    public uint Attributes;
    public IntPtr SecurityDescriptor;
    public IntPtr SecurityQualityOfService;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CLIENT_ID
{
    public IntPtr UniqueProcess;
    public IntPtr UniqueThread;
}
