using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using PersonalRagnarokTool.Core.Infrastructure;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;
using PersonalRagnarokTool.Infrastructure;
using PersonalRagnarokTool.Services;

namespace PersonalRagnarokTool.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly string _configPath;
    private readonly AppConfigStore _configStore;
    private readonly ClientDiscoveryService _discoveryService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly InputDispatcher _inputDispatcher;
    private readonly TurboEngine _turboEngine;
    private readonly ToggleService _toggleService;
    private readonly AppConfig _config;

    private ClientProfile? _selectedClient;
    private MacroBinding? _selectedBinding;
    private MacroStep? _selectedStep;
    private ClientWindowRef? _selectedAvailableWindow;
    private string _statusMessage = "Ready.";
    private string _hotkeyStatus = "Hotkeys are waiting for registration.";
    private string _selectedClientBindingStatus = "Unbound";
    private string _selectedClientBindingDetail = "Bind this profile to a live client window.";
    private string _selectedBindingIntervalMsText = string.Empty;
    private string _bindingEditorValidationMessage = string.Empty;
    private bool _isGlobalActive = true;
    private string _globalToggleStatusText = "ACTIVE";
    private bool _isSidebarCollapsed;
    private bool _isUpdatingBindingEditorFields;
    private MacroChainLane? _selectedSongLane;
    private MacroChainLane? _selectedSwitchLane;

    public MainViewModel(
        string configPath,
        AppConfigStore configStore,
        ClientDiscoveryService discoveryService,
        GlobalHotkeyService hotkeyService,
        InputDispatcher inputDispatcher,
        TurboEngine turboEngine,
        ToggleService toggleService)
    {
        _configPath = configPath;
        _configStore = configStore;
        _discoveryService = discoveryService;
        _hotkeyService = hotkeyService;
        _inputDispatcher = inputDispatcher;
        _turboEngine = turboEngine;
        _toggleService = toggleService;
        _config = _configStore.Load(_configPath);

        AvailableWindows = new ObservableCollection<ClientWindowRef>();

        // Client management
        AddClientCommand = new RelayCommand(AddClient);
        RemoveSelectedClientCommand = new RelayCommand(RemoveSelectedClient, () => SelectedClient is not null);
        SaveCommand = new RelayCommand(SaveConfig);
        RefreshWindowsCommand = new RelayCommand(RefreshWindows);
        BindSelectedClientCommand = new RelayCommand(BindSelectedClient, () => SelectedClient is not null && SelectedAvailableWindow is not null);
        UnbindSelectedClientCommand = new RelayCommand(UnbindSelectedClient, () => SelectedClient?.BoundWindow is not null);
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

        // Macro management
        AddBindingCommand = new RelayCommand(AddBinding, () => SelectedClient is not null);
        RemoveBindingCommand = new RelayCommand(RemoveSelectedBinding, () => SelectedBinding is not null);
        AddStepCommand = new RelayCommand(AddStep, () => SelectedBinding is not null);
        RemoveStepCommand = new RelayCommand(RemoveSelectedStep, () => SelectedStep is not null);

        // Autobuff (legacy)
        AddBuffCommand = new RelayCommand(AddBuff, () => SelectedClient is not null);
        RemoveBuffCommand = new RelayCommand<BuffConfig>(RemoveBuff, _ => SelectedClient is not null);

        // Autobuff Skills
        AddSkillBuffCommand = new RelayCommand(AddSkillBuff, () => SelectedClient is not null);
        RemoveSkillBuffCommand = new RelayCommand<BuffConfig>(RemoveSkillBuff, _ => SelectedClient is not null);

        // Autobuff Items
        AddItemBuffCommand = new RelayCommand(AddItemBuff, () => SelectedClient is not null);
        RemoveItemBuffCommand = new RelayCommand<BuffConfig>(RemoveItemBuff, _ => SelectedClient is not null);

        // Spammer
        AddSpammerKeyCommand = new RelayCommand(AddSpammerKey, () => SelectedClient is not null);
        RemoveSpammerKeyCommand = new RelayCommand<SpammerKey>(RemoveSpammerKey, _ => SelectedClient is not null);

        // Recovery (legacy)
        AddRecoveryCommand = new RelayCommand(AddRecovery, () => SelectedClient is not null);
        RemoveRecoveryCommand = new RelayCommand<RecoveryConfig>(RemoveRecovery, _ => SelectedClient is not null);

        // Debuff Recovery
        AddDebuffRecoveryCommand = new RelayCommand(AddDebuffRecovery, () => SelectedClient is not null);
        RemoveDebuffRecoveryCommand = new RelayCommand<RecoveryConfig>(RemoveDebuffRecovery, _ => SelectedClient is not null);

        // ATK/DEF
        AddAtkKeyCommand = new RelayCommand(AddAtkKey, () => SelectedClient is not null);
        RemoveAtkKeyCommand = new RelayCommand<AtkDefKeyEntry>(RemoveAtkKey, _ => SelectedClient is not null);
        AddDefKeyCommand = new RelayCommand(AddDefKey, () => SelectedClient is not null);
        RemoveDefKeyCommand = new RelayCommand<AtkDefKeyEntry>(RemoveDefKey, _ => SelectedClient is not null);

        // Macro Song / Switch
        AddSongEntryCommand = new RelayCommand(AddSongEntry, () => SelectedSongLane is not null);
        RemoveSongEntryCommand = new RelayCommand<MacroChainEntry>(RemoveSongEntry, _ => SelectedSongLane is not null);
        AddSwitchEntryCommand = new RelayCommand(AddSwitchEntry, () => SelectedSwitchLane is not null);
        RemoveSwitchEntryCommand = new RelayCommand<MacroChainEntry>(RemoveSwitchEntry, _ => SelectedSwitchLane is not null);

        // Server management
        AddServerCommand = new RelayCommand(AddServer);
        RemoveServerCommand = new RelayCommand<ServerEntry>(RemoveServer);

        AttachConfigSubscriptions();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;

        RefreshWindows();
        SelectedClient = _config.ClientProfiles.FirstOrDefault();
        if (SelectedClient is null) AddClient();
    }

    // ═══════════════════════════════════════════════════════════
    //  Client management
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<ClientProfile> ClientProfiles => _config.ClientProfiles;
    public ObservableCollection<ClientWindowRef> AvailableWindows { get; }

    public ClientProfile? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                SelectedBinding = value?.Bindings.FirstOrDefault();
                SelectedSongLane = value?.MacroSongs.Lanes.FirstOrDefault();
                SelectedSwitchLane = value?.MacroSwitch.Lanes.FirstOrDefault();
                RefreshSelectedClientRuntimeState();
                RefreshCommandStates();
            }
        }
    }

    public ClientWindowRef? SelectedAvailableWindow
    {
        get => _selectedAvailableWindow;
        set
        {
            if (SetProperty(ref _selectedAvailableWindow, value))
                RefreshCommandStates();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Binding management
    // ═══════════════════════════════════════════════════════════

    public MacroBinding? SelectedBinding
    {
        get => _selectedBinding;
        set
        {
            if (SetProperty(ref _selectedBinding, value))
            {
                SelectedStep = value?.Steps.FirstOrDefault();
                RefreshBindingEditorFields();
                RefreshCommandStates();
                RaisePropertyChanged(nameof(MacroListHeader));
            }
        }
    }

    public MacroStep? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (SetProperty(ref _selectedStep, value))
                RefreshCommandStates();
        }
    }

    public MacroChainLane? SelectedSongLane
    {
        get => _selectedSongLane;
        set => SetProperty(ref _selectedSongLane, value);
    }

    public MacroChainLane? SelectedSwitchLane
    {
        get => _selectedSwitchLane;
        set => SetProperty(ref _selectedSwitchLane, value);
    }

    public InputMethod[] InputMethods => Enum.GetValues<InputMethod>();

    public InputMethod SelectedInputMethod
    {
        get => _config.InputMethod;
        set
        {
            if (_config.InputMethod != value)
            {
                _config.InputMethod = value;
                RaisePropertyChanged();
                SaveConfig();
            }
        }
    }

    public string MacroListHeader =>
        SelectedBinding is not null
            ? $"Macro List for [{SelectedBinding.TriggerKey}]"
            : "Macro List";

    // ═══════════════════════════════════════════════════════════
    //  Toggle display + reassignment
    // ═══════════════════════════════════════════════════════════

    public string[] ModifierOptions => Infrastructure.ModifierOptions.All;

    public bool IsGlobalActive
    {
        get => _toggleService.IsGlobalActive;
        private set
        {
            if (SetProperty(ref _isGlobalActive, value))
            {
                RaisePropertyChanged(nameof(GlobalToggleStatusText));
            }
        }
    }

    public string GlobalToggleStatusText
    {
        get => _globalToggleStatusText;
        set => SetProperty(ref _globalToggleStatusText, value);
    }

    public string GlobalToggleComboText => _config.GlobalToggle.DisplayText;

    public string GlobalToggleModifier
    {
        get => _config.GlobalToggle.Modifier;
        set
        {
            if (_config.GlobalToggle.Modifier != value)
            {
                _config.GlobalToggle.Modifier = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GlobalToggleComboText));
                RefreshHotkeys();
            }
        }
    }

    public string GlobalToggleKey
    {
        get => _config.GlobalToggle.Key;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(trimmed) && _config.GlobalToggle.Key != trimmed)
            {
                _config.GlobalToggle.Key = trimmed;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GlobalToggleComboText));
                RefreshHotkeys();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Status
    // ═══════════════════════════════════════════════════════════

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string HotkeyStatus
    {
        get => _hotkeyStatus;
        private set => SetProperty(ref _hotkeyStatus, value);
    }

    public string SelectedClientBindingStatus
    {
        get => _selectedClientBindingStatus;
        private set => SetProperty(ref _selectedClientBindingStatus, value);
    }

    public string SelectedClientBindingDetail
    {
        get => _selectedClientBindingDetail;
        private set => SetProperty(ref _selectedClientBindingDetail, value);
    }

    // ═══════════════════════════════════════════════════════════
    //  Numeric field proxies
    // ═══════════════════════════════════════════════════════════

    public string SelectedBindingIntervalMsText
    {
        get => _selectedBindingIntervalMsText;
        set
        {
            if (SetProperty(ref _selectedBindingIntervalMsText, value))
                ApplyBindingNumericFieldChanges();
        }
    }

    public string BindingEditorValidationMessage
    {
        get => _bindingEditorValidationMessage;
        private set => SetProperty(ref _bindingEditorValidationMessage, value);
    }

    // ═══════════════════════════════════════════════════════════
    //  Sidebar
    // ═══════════════════════════════════════════════════════════

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set
        {
            if (SetProperty(ref _isSidebarCollapsed, value))
            {
                RaisePropertyChanged(nameof(SidebarWidth));
                RaisePropertyChanged(nameof(SidebarToggleLabel));
            }
        }
    }

    public double SidebarWidth => IsSidebarCollapsed ? 48 : 260;
    public string SidebarToggleLabel => IsSidebarCollapsed ? ">>" : "<<";

    // ═══════════════════════════════════════════════════════════
    //  Server management
    // ═══════════════════════════════════════════════════════════

    public ObservableCollection<ServerEntry> Servers => _config.Servers.Servers;

    // ═══════════════════════════════════════════════════════════
    //  Buff catalog (static data for UI)
    // ═══════════════════════════════════════════════════════════

    public List<BuffDefinition> AvailableSkillBuffs { get; } = BuffCatalog.GetAllSkillBuffs();
    public List<BuffDefinition> AvailableItemBuffs { get; } = BuffCatalog.GetAllItemBuffs();
    public List<BuffDefinition> AvailableDebuffs { get; } = BuffCatalog.GetDebuffs();
    public List<string> SkillBuffCategories { get; } = BuffCatalog.GetAllSkillBuffs().Select(b => b.Category).Distinct().ToList();
    public List<string> ItemBuffCategories { get; } = BuffCatalog.GetAllItemBuffs().Select(b => b.Category).Distinct().ToList();

    // ═══════════════════════════════════════════════════════════
    //  Commands
    // ═══════════════════════════════════════════════════════════

    public RelayCommand AddClientCommand { get; }
    public RelayCommand RemoveSelectedClientCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshWindowsCommand { get; }
    public RelayCommand BindSelectedClientCommand { get; }
    public RelayCommand UnbindSelectedClientCommand { get; }
    public RelayCommand AddBindingCommand { get; }
    public RelayCommand RemoveBindingCommand { get; }
    public RelayCommand AddStepCommand { get; }
    public RelayCommand RemoveStepCommand { get; }
    public RelayCommand ToggleSidebarCommand { get; }

    // Autobuff (legacy)
    public RelayCommand AddBuffCommand { get; }
    public RelayCommand<BuffConfig> RemoveBuffCommand { get; }

    // Autobuff Skills
    public RelayCommand AddSkillBuffCommand { get; }
    public RelayCommand<BuffConfig> RemoveSkillBuffCommand { get; }

    // Autobuff Items
    public RelayCommand AddItemBuffCommand { get; }
    public RelayCommand<BuffConfig> RemoveItemBuffCommand { get; }

    // Spammer
    public RelayCommand AddSpammerKeyCommand { get; }
    public RelayCommand<SpammerKey> RemoveSpammerKeyCommand { get; }

    // Recovery (legacy)
    public RelayCommand AddRecoveryCommand { get; }
    public RelayCommand<RecoveryConfig> RemoveRecoveryCommand { get; }

    // Debuff Recovery
    public RelayCommand AddDebuffRecoveryCommand { get; }
    public RelayCommand<RecoveryConfig> RemoveDebuffRecoveryCommand { get; }

    // ATK/DEF
    public RelayCommand AddAtkKeyCommand { get; }
    public RelayCommand<AtkDefKeyEntry> RemoveAtkKeyCommand { get; }
    public RelayCommand AddDefKeyCommand { get; }
    public RelayCommand<AtkDefKeyEntry> RemoveDefKeyCommand { get; }

    // Macro Song / Switch
    public RelayCommand AddSongEntryCommand { get; }
    public RelayCommand<MacroChainEntry> RemoveSongEntryCommand { get; }
    public RelayCommand AddSwitchEntryCommand { get; }
    public RelayCommand<MacroChainEntry> RemoveSwitchEntryCommand { get; }

    // Servers
    public RelayCommand AddServerCommand { get; }
    public RelayCommand<ServerEntry> RemoveServerCommand { get; }

    // ═══════════════════════════════════════════════════════════
    //  Feature CRUD
    // ═══════════════════════════════════════════════════════════

    private void AddBuff() => SelectedClient?.Autobuff.Buffs.Add(new BuffConfig { Name = "New Buff" });
    private void RemoveBuff(BuffConfig? buff) { if (buff != null) SelectedClient?.Autobuff.Buffs.Remove(buff); }

    private void AddSkillBuff() => SelectedClient?.AutobuffSkills.Buffs.Add(new BuffConfig { Name = "New Skill Buff" });
    private void RemoveSkillBuff(BuffConfig? buff) { if (buff != null) SelectedClient?.AutobuffSkills.Buffs.Remove(buff); }

    private void AddItemBuff() => SelectedClient?.AutobuffItems.Buffs.Add(new BuffConfig { Name = "New Item Buff" });
    private void RemoveItemBuff(BuffConfig? buff) { if (buff != null) SelectedClient?.AutobuffItems.Buffs.Remove(buff); }

    private void AddSpammerKey() => SelectedClient?.Spammer.Keys.Add(new SpammerKey());
    private void RemoveSpammerKey(SpammerKey? key) { if (key != null) SelectedClient?.Spammer.Keys.Remove(key); }

    private void AddRecovery() => SelectedClient?.Recovery.Recoveries.Add(new RecoveryConfig { Name = "New Status" });
    private void RemoveRecovery(RecoveryConfig? rec) { if (rec != null) SelectedClient?.Recovery.Recoveries.Remove(rec); }

    private void AddDebuffRecovery() => SelectedClient?.DebuffRecovery.Recoveries.Add(new RecoveryConfig { Name = "New Debuff" });
    private void RemoveDebuffRecovery(RecoveryConfig? rec) { if (rec != null) SelectedClient?.DebuffRecovery.Recoveries.Remove(rec); }

    private void AddAtkKey()
    {
        if (SelectedClient is null) return;
        int n = SelectedClient.AtkDefMode.AtkKeys.Count + 1;
        SelectedClient.AtkDefMode.AtkKeys.Add(new AtkDefKeyEntry { SlotName = $"ATK Slot {n}" });
    }
    private void RemoveAtkKey(AtkDefKeyEntry? key) { if (key != null) SelectedClient?.AtkDefMode.AtkKeys.Remove(key); }

    private void AddDefKey()
    {
        if (SelectedClient is null) return;
        int n = SelectedClient.AtkDefMode.DefKeys.Count + 1;
        SelectedClient.AtkDefMode.DefKeys.Add(new AtkDefKeyEntry { SlotName = $"DEF Slot {n}" });
    }
    private void RemoveDefKey(AtkDefKeyEntry? key) { if (key != null) SelectedClient?.AtkDefMode.DefKeys.Remove(key); }

    private void AddSongEntry() => SelectedSongLane?.Entries.Add(new MacroChainEntry());
    private void RemoveSongEntry(MacroChainEntry? entry) { if (entry != null) SelectedSongLane?.Entries.Remove(entry); }

    private void AddSwitchEntry() => SelectedSwitchLane?.Entries.Add(new MacroChainEntry());
    private void RemoveSwitchEntry(MacroChainEntry? entry) { if (entry != null) SelectedSwitchLane?.Entries.Remove(entry); }

    private void AddServer() => Servers.Add(new ServerEntry());
    private void RemoveServer(ServerEntry? server) { if (server != null) Servers.Remove(server); }

    public void AddBuffFromCatalog(BuffDefinition def, string target)
    {
        if (SelectedClient is null) return;
        var buff = new BuffConfig { Name = def.Name, StatusId = def.StatusId, Key = "None", Enabled = true };
        if (target == "skill")
            SelectedClient.AutobuffSkills.Buffs.Add(buff);
        else if (target == "item")
            SelectedClient.AutobuffItems.Buffs.Add(buff);
    }

    public void AddDebuffFromCatalog(BuffDefinition def)
    {
        if (SelectedClient is null) return;
        SelectedClient.DebuffRecovery.Recoveries.Add(new RecoveryConfig
        {
            Name = def.Name,
            StatusId = def.StatusId,
            Key = "None",
            Enabled = true
        });
    }

    // ═══════════════════════════════════════════════════════════
    //  4R Feature Sync
    // ═══════════════════════════════════════════════════════════

    private void Sync4RFeatures(ClientProfile profile)
    {
        if (profile?.BoundWindow == null) return;
        int pid = profile.BoundWindow.ProcessId;

        AgentPipeClient.SyncAutopot(pid, profile.Autopot, profile.YggAutopot);
        AgentPipeClient.SyncAutobuff(pid, profile.Autobuff);
        AgentPipeClient.SyncAutobuffSkills(pid, profile.AutobuffSkills);
        AgentPipeClient.SyncAutobuffItems(pid, profile.AutobuffItems);
        AgentPipeClient.SyncSpammer(pid, profile.Spammer);
        AgentPipeClient.SyncRecovery(pid, profile.Recovery);
        AgentPipeClient.SyncDebuffRecovery(pid, profile.DebuffRecovery);
        AgentPipeClient.SyncSkillTimers(pid, profile.SkillTimers);
        AgentPipeClient.SyncAtkDefMode(pid, profile.AtkDefMode);
        AgentPipeClient.SyncMacroSongs(pid, profile.MacroSongs);
        AgentPipeClient.SyncMacroSwitch(pid, profile.MacroSwitch);
    }

    // ═══════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════

    public void AttachWindowHandle(IntPtr windowHandle) { }
    public void RefreshHotkeyPolling() => RefreshHotkeys();

    // ═══════════════════════════════════════════════════════════
    //  Binding editor helpers
    // ═══════════════════════════════════════════════════════════

    private void RefreshBindingEditorFields()
    {
        _isUpdatingBindingEditorFields = true;
        SelectedBindingIntervalMsText = SelectedBinding?.IntervalMs.ToString() ?? string.Empty;
        BindingEditorValidationMessage = string.Empty;
        _isUpdatingBindingEditorFields = false;
    }

    private void ApplyBindingNumericFieldChanges()
    {
        if (_isUpdatingBindingEditorFields || SelectedBinding is null) return;
        if (TryParseInt(SelectedBindingIntervalMsText, out int intervalMs) && intervalMs >= 25)
            SelectedBinding.IntervalMs = intervalMs;
        else
            BindingEditorValidationMessage = "Interval must be an integer >= 25.";
    }

    private static bool TryParseInt(string value, out int result)
    {
        result = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out result);
    }

    // ═══════════════════════════════════════════════════════════
    //  Client / binding / step CRUD
    // ═══════════════════════════════════════════════════════════

    private void AddClient()
    {
        int number = ClientProfiles.Count + 1;
        var profile = new ClientProfile { DisplayName = $"Client {number}" };
        var binding = new MacroBinding
        {
            ClientProfileId = profile.Id,
            Name = $"{profile.DisplayName} Macro 1"
        };
        binding.Steps.Add(new MacroStep { Key = "F1", DelayMs = 100 });
        profile.Bindings.Add(binding);
        ClientProfiles.Add(profile);
        SelectedClient = profile;
        StatusMessage = $"{profile.DisplayName} added.";
    }

    private void RemoveSelectedClient()
    {
        if (SelectedClient is null) return;
        int index = ClientProfiles.IndexOf(SelectedClient);
        string displayName = SelectedClient.DisplayName;
        ClientProfiles.Remove(SelectedClient);
        SelectedClient = ClientProfiles.Count == 0
            ? null
            : ClientProfiles[Math.Clamp(index - 1, 0, ClientProfiles.Count - 1)];
        StatusMessage = $"{displayName} removed.";
    }

    private void SaveConfig()
    {
        _configStore.Save(_configPath, _config);
        RefreshHotkeys();
        StatusMessage = $"Configuration saved to {_configPath}.";
    }

    private void RefreshWindows()
    {
        AvailableWindows.Clear();
        foreach (var window in _discoveryService.DiscoverWindows())
            AvailableWindows.Add(window);

        RefreshAllClientRuntimeStates();
        SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient?.BoundWindow)
            ?? AvailableWindows.FirstOrDefault();

        StatusMessage = AvailableWindows.Count == 0
            ? "No windows discovered."
            : $"Discovered {AvailableWindows.Count} window(s). Selected client status: {SelectedClientBindingStatus}.";
    }

    private void BindSelectedClient()
    {
        if (SelectedClient is null || SelectedAvailableWindow is null) return;
        SelectedClient.BoundWindow = SelectedAvailableWindow;
        RefreshSelectedClientRuntimeState();

        try
        {
            int pid = SelectedAvailableWindow.ProcessId;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] stealthPayloads = { "HEAVENSGATE.bin", "ColdHide.bin", "Dll1.bin" };

            foreach (var payload in stealthPayloads)
            {
                string payloadPath = Path.Combine(baseDir, payload);
                if (File.Exists(payloadPath))
                {
                    bool success = ManualMapInjector.Inject(pid, payloadPath);
                    StatusMessage = success
                        ? $"Stealth Injected: {payload}"
                        : $"Failed to inject {payload}. Check logs.";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Injection Error: {ex.Message}";
        }

        StatusMessage = $"{SelectedClient.DisplayName} bound to {SelectedClient.BoundWindowDisplayText}.";
    }

    private void UnbindSelectedClient()
    {
        if (SelectedClient is null) return;
        SelectedClient.BoundWindow = null;
        RefreshSelectedClientRuntimeState();
        StatusMessage = $"{SelectedClient.DisplayName} unbound from any client window.";
    }

    private void AddBinding()
    {
        if (SelectedClient is null) return;
        var binding = new MacroBinding
        {
            ClientProfileId = SelectedClient.Id,
            Name = $"{SelectedClient.DisplayName} Macro {SelectedClient.Bindings.Count + 1}"
        };
        binding.Steps.Add(new MacroStep { Key = "F1", DelayMs = 100 });
        SelectedClient.Bindings.Add(binding);
        SelectedBinding = binding;
        StatusMessage = $"{SelectedClient.DisplayName}: macro '{binding.Name}' added.";
    }

    private void RemoveSelectedBinding()
    {
        if (SelectedClient is null || SelectedBinding is null) return;
        string name = SelectedBinding.Name;
        SelectedClient.Bindings.Remove(SelectedBinding);
        SelectedBinding = SelectedClient.Bindings.FirstOrDefault();
        StatusMessage = $"{SelectedClient.DisplayName}: macro '{name}' removed.";
    }

    private void AddStep()
    {
        if (SelectedBinding is null) return;
        var step = new MacroStep { Key = "F1", DelayMs = 100 };
        SelectedBinding.Steps.Add(step);
        SelectedStep = step;
        StatusMessage = $"Step added to {SelectedBinding.Name}.";
    }

    private void RemoveSelectedStep()
    {
        if (SelectedBinding is null || SelectedStep is null) return;
        SelectedBinding.Steps.Remove(SelectedStep);
        SelectedStep = SelectedBinding.Steps.LastOrDefault();
        StatusMessage = $"Step removed from {SelectedBinding.Name}.";
    }

    // ═══════════════════════════════════════════════════════════
    //  Runtime state
    // ═══════════════════════════════════════════════════════════

    private void RefreshSelectedClientRuntimeState()
    {
        if (SelectedClient is null)
        {
            SelectedClientBindingStatus = "Unbound";
            SelectedClientBindingDetail = "Select a client profile.";
            return;
        }

        if (SelectedClient.BoundWindow is null)
        {
            SelectedClient.HasLiveWindow = false;
            SelectedClient.RuntimeStatusLabel = "Unbound";
            SelectedClient.RuntimeStatusDetail = "Bind this profile to a live client window.";
            SelectedClientBindingStatus = "Unbound";
            SelectedClientBindingDetail = "Bind this profile to a live client window.";
            return;
        }

        IntPtr hwnd = new(SelectedClient.BoundWindow.WindowHandle);
        bool isAlive = hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd);

        SelectedClient.HasLiveWindow = isAlive;
        if (isAlive)
        {
            SelectedClient.RuntimeStatusLabel = "Live";
            SelectedClient.RuntimeStatusDetail = $"Bound to {SelectedClient.BoundWindowDisplayText}.";
        }
        else
        {
            SelectedClient.RuntimeStatusLabel = "Stale";
            SelectedClient.RuntimeStatusDetail = "Window handle is no longer valid. Rebind or refresh.";
        }

        SelectedClientBindingStatus = SelectedClient.RuntimeStatusLabel;
        SelectedClientBindingDetail = SelectedClient.RuntimeStatusDetail;
    }

    private void RefreshAllClientRuntimeStates()
    {
        foreach (var profile in ClientProfiles)
        {
            if (profile.BoundWindow is null)
            {
                profile.HasLiveWindow = false;
                profile.RuntimeStatusLabel = "Unbound";
                profile.RuntimeStatusDetail = "Bind this profile to a live client window.";
                continue;
            }

            IntPtr hwnd = new(profile.BoundWindow.WindowHandle);
            bool isAlive = hwnd != IntPtr.Zero && NativeMethods.IsWindow(hwnd);

            profile.HasLiveWindow = isAlive;
            if (isAlive)
            {
                profile.RuntimeStatusLabel = "Live";
                profile.RuntimeStatusDetail = $"Bound to {profile.BoundWindowDisplayText}.";
            }
            else
            {
                profile.RuntimeStatusLabel = "Stale";
                profile.RuntimeStatusDetail = "Window handle is no longer valid. Rebind or refresh.";
            }
        }

        RefreshSelectedClientRuntimeState();
    }

    // ═══════════════════════════════════════════════════════════
    //  Config subscriptions
    // ═══════════════════════════════════════════════════════════

    private void AttachConfigSubscriptions()
    {
        ClientProfiles.CollectionChanged += OnProfilesCollectionChanged;
        foreach (var profile in ClientProfiles)
            AttachProfileSubscriptions(profile);
    }

    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (ClientProfile profile in e.NewItems)
                AttachProfileSubscriptions(profile);

        if (e.OldItems is not null)
            foreach (ClientProfile profile in e.OldItems)
                DetachProfileSubscriptions(profile);

        RefreshHotkeys();
        RefreshCommandStates();
    }

    private void AttachProfileSubscriptions(ClientProfile profile)
    {
        profile.PropertyChanged += OnProfilePropertyChanged;
        profile.Bindings.CollectionChanged += OnBindingsCollectionChanged;
        foreach (var binding in profile.Bindings)
            binding.PropertyChanged += OnBindingPropertyChanged;

        // 4R Feature Subscriptions
        void SyncAll(object? s, object e) => Sync4RFeatures(profile);

        profile.Autopot.PropertyChanged += SyncAll;
        profile.YggAutopot.PropertyChanged += SyncAll;
        profile.SkillTimers.PropertyChanged += SyncAll;
        profile.SkillTimers.Timer1.PropertyChanged += SyncAll;
        profile.SkillTimers.Timer2.PropertyChanged += SyncAll;
        profile.SkillTimers.Timer3.PropertyChanged += SyncAll;
        profile.AtkDefMode.PropertyChanged += SyncAll;
        profile.MacroSongs.PropertyChanged += SyncAll;
        profile.MacroSwitch.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.Autobuff.Buffs, profile);
        profile.Autobuff.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.AutobuffSkills.Buffs, profile);
        profile.AutobuffSkills.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.AutobuffItems.Buffs, profile);
        profile.AutobuffItems.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.Spammer.Keys, profile);
        profile.Spammer.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.Recovery.Recoveries, profile);
        profile.Recovery.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.DebuffRecovery.Recoveries, profile);
        profile.DebuffRecovery.PropertyChanged += SyncAll;

        AttachCollectionSync(profile.AtkDefMode.AtkKeys, profile);
        AttachCollectionSync(profile.AtkDefMode.DefKeys, profile);

        foreach (var lane in profile.MacroSongs.Lanes)
        {
            lane.PropertyChanged += SyncAll;
            AttachCollectionSync(lane.Entries, profile);
        }

        foreach (var lane in profile.MacroSwitch.Lanes)
        {
            lane.PropertyChanged += SyncAll;
            AttachCollectionSync(lane.Entries, profile);
        }
    }

    private void AttachCollectionSync<T>(ObservableCollection<T> collection, ClientProfile profile) where T : ObservableObject
    {
        collection.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
                foreach (T item in e.NewItems)
                    item.PropertyChanged += (s2, e2) => Sync4RFeatures(profile);
            Sync4RFeatures(profile);
        };
    }

    private void DetachProfileSubscriptions(ClientProfile profile)
    {
        profile.PropertyChanged -= OnProfilePropertyChanged;
        profile.Bindings.CollectionChanged -= OnBindingsCollectionChanged;
        foreach (var binding in profile.Bindings)
            binding.PropertyChanged -= OnBindingPropertyChanged;
    }

    private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClientProfile.IsEnabled) or nameof(ClientProfile.BoundWindow))
            RefreshHotkeys();
    }

    private void OnBindingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (MacroBinding binding in e.NewItems)
                binding.PropertyChanged += OnBindingPropertyChanged;

        if (e.OldItems is not null)
            foreach (MacroBinding binding in e.OldItems)
                binding.PropertyChanged -= OnBindingPropertyChanged;

        RefreshHotkeys();
        RefreshCommandStates();
    }

    private void OnBindingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MacroBinding.TriggerKey))
        {
            RefreshHotkeys();
            RaisePropertyChanged(nameof(MacroListHeader));
        }

        if (e.PropertyName is nameof(MacroBinding.IsEnabled))
            RefreshHotkeys();
    }

    // ═══════════════════════════════════════════════════════════
    //  Hotkey polling
    // ═══════════════════════════════════════════════════════════

    private void RefreshHotkeys()
    {
        if (!_hotkeyService.IsReady)
        {
            HotkeyStatus = "Waiting for polling thread...";
            return;
        }

        var clientToggles = new List<(ToggleCombo combo, string tag)>();
        foreach (var profile in ClientProfiles)
        {
            if (profile.ClientToggle is not null)
                clientToggles.Add((profile.ClientToggle, $"TOGGLE:client:{profile.Id}"));
        }

        var errors = _hotkeyService.RefreshMonitoredKeys(
            ClientProfiles
                .Where(p => p.IsEnabled && p.HasLiveWindow)
                .SelectMany(p => p.Bindings.Where(b => b.IsEnabled)),
            _config.GlobalToggle,
            clientToggles);

        HotkeyStatus = errors.Count == 0
            ? $"Polling active. Toggle: {_config.GlobalToggle.DisplayText}"
            : string.Join(" | ", errors);
    }

    // ═══════════════════════════════════════════════════════════
    //  Hotkey execution
    // ═══════════════════════════════════════════════════════════

    private void OnHotkeyPressed(object? sender, string hotkey)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (hotkey == "TOGGLE:global")
            {
                bool nowActive = _toggleService.ToggleGlobal();
                if (!nowActive) _turboEngine.StopAll();
                IsGlobalActive = nowActive;
                GlobalToggleStatusText = nowActive ? "ACTIVE" : "PAUSED";
                StatusMessage = nowActive ? "Macros ACTIVATED" : "Macros PAUSED \u2014 all turbos stopped.";

                foreach (var p in ClientProfiles)
                    p.IsActive = _toggleService.IsClientActive(p.Id);
                return;
            }

            if (hotkey.StartsWith("TOGGLE:client:"))
            {
                string profileId = hotkey["TOGGLE:client:".Length..];
                bool nowActive = _toggleService.ToggleClient(profileId);
                var profile = ClientProfiles.FirstOrDefault(p => p.Id == profileId);
                if (profile != null)
                {
                    profile.IsActive = nowActive;
                    if (!nowActive)
                    {
                        foreach (var b in profile.Bindings)
                            _turboEngine.StopTurbo(b.Id);
                    }
                    StatusMessage = $"{profile.DisplayName}: macros {(nowActive ? "ACTIVATED" : "PAUSED")}";
                }
                return;
            }

            if (!_toggleService.IsGlobalActive) return;

            string normalizedHotkey = HotkeyText.Normalize(hotkey);
            MacroBinding? binding = null;
            ClientProfile? ownerProfile = null;
            foreach (var profile in ClientProfiles)
            {
                if (!profile.IsEnabled || !profile.HasLiveWindow) continue;
                if (!_toggleService.IsClientActive(profile.Id)) continue;
                var match = profile.Bindings.FirstOrDefault(b =>
                    b.IsEnabled && HotkeyText.Normalize(b.TriggerKey) == normalizedHotkey);
                if (match != null)
                {
                    binding = match;
                    ownerProfile = profile;
                    break;
                }
            }

            if (binding is null || ownerProfile?.BoundWindow is null) return;
            if (binding.Steps.Count == 0) return;

            var window = ownerProfile.BoundWindow;

            if (_turboEngine.IsRunning(binding.Id))
            {
                _turboEngine.StopTurbo(binding.Id);
                StatusMessage = $"{ownerProfile.DisplayName}: STOPPED \u2014 {binding.Name}";
            }
            else
            {
                _turboEngine.StartTurbo(binding, window, _config.InputMethod);
                StatusMessage = $"{ownerProfile.DisplayName}: STARTED \u2014 {binding.Name} ({binding.Steps.Count} steps, {binding.IntervalMs}ms)";
            }
        });
    }

    // ═══════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════

    private ClientWindowRef? FindMatchingAvailableWindow(ClientWindowRef? boundWindow) =>
        ClientWindowMatcher.Match(boundWindow, AvailableWindows).ResolvedWindow;

    private void RefreshCommandStates()
    {
        RemoveSelectedClientCommand.RaiseCanExecuteChanged();
        BindSelectedClientCommand.RaiseCanExecuteChanged();
        UnbindSelectedClientCommand.RaiseCanExecuteChanged();
        AddBindingCommand.RaiseCanExecuteChanged();
        RemoveBindingCommand.RaiseCanExecuteChanged();
        AddStepCommand.RaiseCanExecuteChanged();
        RemoveStepCommand.RaiseCanExecuteChanged();
    }

    // ═══════════════════════════════════════════════════════════
    //  Dispose
    // ═══════════════════════════════════════════════════════════

    public void Dispose()
    {
        SaveConfig();
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.Stop();
        _turboEngine.StopAll();
    }
}
