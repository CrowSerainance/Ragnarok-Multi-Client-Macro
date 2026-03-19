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

        AddClientCommand = new RelayCommand(AddClient);
        RemoveSelectedClientCommand = new RelayCommand(RemoveSelectedClient, () => SelectedClient is not null);
        SaveCommand = new RelayCommand(SaveConfig);
        RefreshWindowsCommand = new RelayCommand(RefreshWindows);
        BindSelectedClientCommand = new RelayCommand(BindSelectedClient, () => SelectedClient is not null && SelectedAvailableWindow is not null);
        UnbindSelectedClientCommand = new RelayCommand(UnbindSelectedClient, () => SelectedClient?.BoundWindow is not null);
        AddBindingCommand = new RelayCommand(AddBinding, () => SelectedClient is not null);
        RemoveBindingCommand = new RelayCommand(RemoveSelectedBinding, () => SelectedBinding is not null);
        AddStepCommand = new RelayCommand(AddStep, () => SelectedBinding is not null);
        RemoveStepCommand = new RelayCommand(RemoveSelectedStep, () => SelectedStep is not null);
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

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

    // ═══════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════

    public void AttachWindowHandle(IntPtr windowHandle)
    {
        // Window handle kept for other uses; hotkey detection is now
        // GetAsyncKeyState polling — no OS hotkey registration needed.
    }

    /// <summary>Called after the polling thread starts to feed it the initial key map.</summary>
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

        // Automatic Stealth Injection
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
    //  Hotkey polling (GetAsyncKeyState — like YXExt.dll)
    // ═══════════════════════════════════════════════════════════

    private void RefreshHotkeys()
    {
        if (!_hotkeyService.IsReady)
        {
            HotkeyStatus = "Waiting for polling thread...";
            return;
        }

        // Build per-client toggle list
        var clientToggles = new List<(ToggleCombo combo, string tag)>();
        foreach (var profile in ClientProfiles)
        {
            if (profile.ClientToggle is not null)
                clientToggles.Add((profile.ClientToggle, $"TOGGLE:client:{profile.Id}"));
        }

        // Feed all monitored keys into the polling thread
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
            // Toggle handling
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

            // System must be active
            if (!_toggleService.IsGlobalActive) return;

            // Find binding
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

            // Toggle turbo: press to start, press again to stop
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
        boundWindow is null
            ? null
            : AvailableWindows.FirstOrDefault(w => w.WindowHandle == boundWindow.WindowHandle)
              ?? AvailableWindows.FirstOrDefault(w => w.ProcessId == boundWindow.ProcessId
                  && string.Equals(w.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase))
              ?? AvailableWindows.FirstOrDefault(w => w.ProcessId == boundWindow.ProcessId);

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
