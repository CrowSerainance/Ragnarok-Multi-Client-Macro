using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using PersonalRagnarokTool.Core.Geometry;
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
    private readonly ClientBindingService _bindingService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly MacroExecutor _macroExecutor;
    private readonly AppConfig _config;
    private ClientProfile? _selectedClient;
    private MacroBinding? _selectedBinding;
    private ClientWindowRef? _selectedAvailableWindow;
    private string _statusMessage = "Ready.";
    private string _hotkeyStatus = "Hotkeys are waiting for registration.";
    private string _selectedClientBindingStatus = "Unbound";
    private string _selectedClientBindingDetail = "Bind this profile to a live client window.";
    private string _selectedBindingPostInputDelayText = string.Empty;
    private string _selectedBindingInterClickDelayText = string.Empty;
    private string _selectedBindingClickCountText = string.Empty;
    private string _bindingEditorValidationMessage = string.Empty;
    private bool _isSidebarCollapsed;
    private bool _isUpdatingBindingEditorFields;

    public MainViewModel(string configPath, AppConfigStore configStore, ClientDiscoveryService discoveryService, ClientBindingService bindingService, GlobalHotkeyService hotkeyService, MacroExecutor macroExecutor)
    {
        _configPath = configPath;
        _configStore = configStore;
        _discoveryService = discoveryService;
        _bindingService = bindingService;
        _hotkeyService = hotkeyService;
        _macroExecutor = macroExecutor;
        _config = _configStore.Load(_configPath);
        AvailableWindows = new ObservableCollection<ClientWindowRef>();
        AddClientCommand = new RelayCommand(AddClient);
        RemoveSelectedClientCommand = new RelayCommand(RemoveSelectedClient, () => SelectedClient is not null);
        SaveCommand = new RelayCommand(SaveConfig);
        RefreshWindowsCommand = new RelayCommand(RefreshWindows);
        BindSelectedClientCommand = new RelayCommand(BindSelectedClient, () => SelectedClient is not null && SelectedAvailableWindow is not null);
        UnbindSelectedClientCommand = new RelayCommand(UnbindSelectedClient, () => SelectedClient?.BoundWindow is not null);
        ResolveSelectedClientCommand = new RelayCommand(ResolveSelectedClient, () => SelectedClient?.BoundWindow is not null);
        AddBindingCommand = new RelayCommand(AddBinding, () => SelectedClient is not null);
        RemoveBindingCommand = new RelayCommand(RemoveSelectedBinding, () => SelectedBinding is not null);
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        AttachConfigSubscriptions();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        RefreshWindows();
        SelectedClient = _config.ClientProfiles.FirstOrDefault();
        if (SelectedClient is null) AddClient();
    }

    public ObservableCollection<ClientProfile> ClientProfiles => _config.ClientProfiles;
    public ObservableCollection<ClientWindowRef> AvailableWindows { get; }
    public InputMethod[] InputMethods => Enum.GetValues<InputMethod>();
    public int[] CellRadiusOptions => CellMath.AllowedRadii;
    public InputMethod SelectedInputMethod { get => _config.InputMethod; set { if (_config.InputMethod != value) { _config.InputMethod = value; RaisePropertyChanged(); SaveConfig(); } } }
    public RelayCommand AddClientCommand { get; }
    public RelayCommand RemoveSelectedClientCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshWindowsCommand { get; }
    public RelayCommand BindSelectedClientCommand { get; }
    public RelayCommand UnbindSelectedClientCommand { get; }
    public RelayCommand ResolveSelectedClientCommand { get; }
    public RelayCommand AddBindingCommand { get; }
    public RelayCommand RemoveBindingCommand { get; }
    public RelayCommand ToggleSidebarCommand { get; }
    public ClientProfile? SelectedClient { get => _selectedClient; set { if (SetProperty(ref _selectedClient, value)) { SelectedBinding = value?.Bindings.FirstOrDefault(); SelectedAvailableWindow = FindMatchingAvailableWindow(value?.BoundWindow); RefreshSelectedClientRuntimeState(true); RefreshCommandStates(); } } }
    public MacroBinding? SelectedBinding { get => _selectedBinding; set { if (SetProperty(ref _selectedBinding, value)) { RefreshBindingEditorFields(); RefreshCommandStates(); } } }
    public ClientWindowRef? SelectedAvailableWindow { get => _selectedAvailableWindow; set { if (SetProperty(ref _selectedAvailableWindow, value)) RefreshCommandStates(); } }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string HotkeyStatus { get => _hotkeyStatus; private set => SetProperty(ref _hotkeyStatus, value); }
    public string SelectedClientBindingStatus { get => _selectedClientBindingStatus; private set => SetProperty(ref _selectedClientBindingStatus, value); }
    public string SelectedClientBindingDetail { get => _selectedClientBindingDetail; private set => SetProperty(ref _selectedClientBindingDetail, value); }
    public string SelectedBindingPostInputDelayText { get => _selectedBindingPostInputDelayText; set { if (SetProperty(ref _selectedBindingPostInputDelayText, value)) ApplyBindingNumericFieldChanges(); } }
    public string SelectedBindingInterClickDelayText { get => _selectedBindingInterClickDelayText; set { if (SetProperty(ref _selectedBindingInterClickDelayText, value)) ApplyBindingNumericFieldChanges(); } }
    public string SelectedBindingClickCountText { get => _selectedBindingClickCountText; set { if (SetProperty(ref _selectedBindingClickCountText, value)) ApplyBindingNumericFieldChanges(); } }
    public string BindingEditorValidationMessage { get => _bindingEditorValidationMessage; private set => SetProperty(ref _bindingEditorValidationMessage, value); }
    public bool IsSidebarCollapsed { get => _isSidebarCollapsed; set { if (SetProperty(ref _isSidebarCollapsed, value)) { RaisePropertyChanged(nameof(SidebarWidth)); RaisePropertyChanged(nameof(SidebarToggleLabel)); } } }
    public double SidebarWidth => IsSidebarCollapsed ? 48 : 244;
    public string SidebarToggleLabel => IsSidebarCollapsed ? ">>" : "<<";

    public void AttachWindowHandle(IntPtr windowHandle) { _hotkeyService.SetWindowHandle(windowHandle); RefreshHotkeys(); }

    private void RefreshBindingEditorFields()
    {
        _isUpdatingBindingEditorFields = true;
        SelectedBindingPostInputDelayText = SelectedBinding?.PostInputDelayMs.ToString() ?? string.Empty;
        SelectedBindingInterClickDelayText = SelectedBinding?.InterClickDelayMs.ToString() ?? string.Empty;
        SelectedBindingClickCountText = SelectedBinding?.ClickCount.ToString() ?? string.Empty;
        BindingEditorValidationMessage = string.Empty;
        _isUpdatingBindingEditorFields = false;
    }

    private void ApplyBindingNumericFieldChanges()
    {
        if (_isUpdatingBindingEditorFields || SelectedBinding is null) return;
        var issues = new List<string>();
        if (TryParseRequiredNonNegativeInt(SelectedBindingPostInputDelayText, out int postDelay)) SelectedBinding.PostInputDelayMs = postDelay; else issues.Add("Post-input delay must be a non-negative integer.");
        if (TryParseRequiredNonNegativeInt(SelectedBindingInterClickDelayText, out int interDelay)) SelectedBinding.InterClickDelayMs = interDelay; else issues.Add("Inter-click delay must be a non-negative integer.");
        if (TryParsePositiveInt(SelectedBindingClickCountText, out int clickCount)) SelectedBinding.ClickCount = clickCount; else issues.Add("Click count must be a positive integer.");
        BindingEditorValidationMessage = string.Join(" ", issues);
    }

    private static bool TryParseRequiredNonNegativeInt(string value, out int parsedValue)
    {
        parsedValue = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsedValue) && parsedValue >= 0;
    }

    private static bool TryParsePositiveInt(string value, out int parsedValue)
    {
        parsedValue = 1;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsedValue) && parsedValue >= 1;
    }

    private void RefreshSelectedClientRuntimeState(bool allowAutoRebind)
    {
        if (SelectedClient is null) { SelectedClientBindingStatus = "Unbound"; SelectedClientBindingDetail = "Select a client profile."; return; }
        var resolution = _bindingService.GetResolution(SelectedClient, AvailableWindows.ToArray(), allowAutoRebind);
        SelectedClientBindingStatus = resolution.Label;
        SelectedClientBindingDetail = resolution.Detail;
        SelectedAvailableWindow = resolution.ResolvedWindow ?? FindMatchingAvailableWindow(SelectedClient.BoundWindow) ?? SelectedAvailableWindow;
    }

    private void RefreshAllClientRuntimeStates(bool allowAutoRebind) { var liveWindows = AvailableWindows.ToArray(); foreach (var profile in ClientProfiles) _bindingService.GetResolution(profile, liveWindows, allowAutoRebind); RefreshSelectedClientRuntimeState(allowAutoRebind); }
    private void AddClient() { int number = ClientProfiles.Count + 1; var profile = new ClientProfile { DisplayName = $"Client {number}" }; profile.Bindings.Add(new MacroBinding { ClientProfileId = profile.Id, Name = $"{profile.DisplayName} Binding 1" }); ClientProfiles.Add(profile); SelectedClient = profile; StatusMessage = $"{profile.DisplayName} added."; }
    private void RemoveSelectedClient() { if (SelectedClient is null) return; int index = ClientProfiles.IndexOf(SelectedClient); string displayName = SelectedClient.DisplayName; ClientProfiles.Remove(SelectedClient); SelectedClient = ClientProfiles.Count == 0 ? null : ClientProfiles[Math.Clamp(index - 1, 0, ClientProfiles.Count - 1)]; StatusMessage = $"{displayName} removed."; }
    private void SaveConfig() { _configStore.Save(_configPath, _config); RefreshAllClientRuntimeStates(false); RefreshHotkeys(); StatusMessage = $"Configuration saved to {_configPath}."; }
    private void RefreshWindows() { AvailableWindows.Clear(); foreach (var window in _discoveryService.DiscoverWindows()) AvailableWindows.Add(window); RefreshAllClientRuntimeStates(true); SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient?.BoundWindow) ?? AvailableWindows.FirstOrDefault(); StatusMessage = AvailableWindows.Count == 0 ? "No windows discovered." : $"Discovered {AvailableWindows.Count} window(s). Selected client status: {SelectedClientBindingStatus}."; }
    private void BindSelectedClient() { if (SelectedClient is null || SelectedAvailableWindow is null) return; _bindingService.BindProfile(SelectedClient, SelectedAvailableWindow); RefreshSelectedClientRuntimeState(true); SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient.BoundWindow); StatusMessage = $"{SelectedClient.DisplayName} bound to {SelectedClient.BoundWindowDisplayText}."; }
    private void UnbindSelectedClient() { if (SelectedClient is null) return; _bindingService.ClearBinding(SelectedClient); RefreshSelectedClientRuntimeState(false); StatusMessage = $"{SelectedClient.DisplayName} unbound from any client window."; }
    private void ResolveSelectedClient() { if (SelectedClient is null) return; RefreshSelectedClientRuntimeState(true); StatusMessage = $"{SelectedClient.DisplayName}: {SelectedClientBindingDetail}"; }
    private void AddBinding() { if (SelectedClient is null) return; var binding = new MacroBinding { ClientProfileId = SelectedClient.Id, Name = $"{SelectedClient.DisplayName} Binding {SelectedClient.Bindings.Count + 1}" }; SelectedClient.Bindings.Add(binding); SelectedBinding = binding; StatusMessage = $"{SelectedClient.DisplayName}: binding '{binding.Name}' added."; }
    private void RemoveSelectedBinding() { if (SelectedClient is null || SelectedBinding is null) return; string name = SelectedBinding.Name; SelectedClient.Bindings.Remove(SelectedBinding); SelectedBinding = SelectedClient.Bindings.FirstOrDefault(); StatusMessage = $"{SelectedClient.DisplayName}: binding '{name}' removed."; }

    private void AttachConfigSubscriptions() { ClientProfiles.CollectionChanged += OnProfilesCollectionChanged; foreach (var profile in ClientProfiles) AttachProfileSubscriptions(profile); }
    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { if (e.NewItems is not null) foreach (ClientProfile profile in e.NewItems) AttachProfileSubscriptions(profile); if (e.OldItems is not null) foreach (ClientProfile profile in e.OldItems) DetachProfileSubscriptions(profile); BindingValidator.NormalizeConfig(_config); RefreshHotkeys(); RefreshCommandStates(); }
    private void AttachProfileSubscriptions(ClientProfile profile) { profile.PropertyChanged += OnProfilePropertyChanged; profile.Bindings.CollectionChanged += OnBindingsCollectionChanged; foreach (var binding in profile.Bindings) binding.PropertyChanged += OnBindingPropertyChanged; }
    private void DetachProfileSubscriptions(ClientProfile profile) { profile.PropertyChanged -= OnProfilePropertyChanged; profile.Bindings.CollectionChanged -= OnBindingsCollectionChanged; foreach (var binding in profile.Bindings) binding.PropertyChanged -= OnBindingPropertyChanged; }
    private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(ClientProfile.IsEnabled) or nameof(ClientProfile.BoundWindow)) RefreshHotkeys(); }
    private void OnBindingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { if (e.NewItems is not null) foreach (MacroBinding binding in e.NewItems) binding.PropertyChanged += OnBindingPropertyChanged; if (e.OldItems is not null) foreach (MacroBinding binding in e.OldItems) binding.PropertyChanged -= OnBindingPropertyChanged; BindingValidator.NormalizeConfig(_config); RefreshHotkeys(); RefreshCommandStates(); }
    private void OnBindingPropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(MacroBinding.TriggerHotkey) or nameof(MacroBinding.IsEnabled)) RefreshHotkeys(); }

    private void RefreshHotkeys()
    {
        BindingValidator.NormalizeConfig(_config);
        if (!_hotkeyService.IsReady) { HotkeyStatus = "Hotkeys will register when the main window is ready."; return; }
        var duplicates = BindingValidator.GetDuplicateHotkeys(_config);
        if (duplicates.Count > 0) { _hotkeyService.UnregisterAll(); HotkeyStatus = $"Duplicate hotkeys: {string.Join(", ", duplicates)}"; return; }
        var errors = _hotkeyService.RegisterBindings(ClientProfiles.SelectMany(profile => profile.Bindings));
        HotkeyStatus = errors.Count == 0 ? "Global hotkeys registered." : string.Join(" | ", errors);
    }

    private async void OnHotkeyPressed(object? sender, string hotkey) { string status = await _macroExecutor.ExecuteHotkeyAsync(_config, hotkey); await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => StatusMessage = status); }
    private ClientWindowRef? FindMatchingAvailableWindow(ClientWindowRef? boundWindow) => boundWindow is null ? null : AvailableWindows.FirstOrDefault(window => window.WindowHandle == boundWindow.WindowHandle) ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId && string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase)) ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId);
    private void RefreshCommandStates() { RemoveSelectedClientCommand.RaiseCanExecuteChanged(); BindSelectedClientCommand.RaiseCanExecuteChanged(); UnbindSelectedClientCommand.RaiseCanExecuteChanged(); ResolveSelectedClientCommand.RaiseCanExecuteChanged(); AddBindingCommand.RaiseCanExecuteChanged(); RemoveBindingCommand.RaiseCanExecuteChanged(); }
    public void Dispose() { SaveConfig(); _hotkeyService.HotkeyPressed -= OnHotkeyPressed; _hotkeyService.UnregisterAll(); }
}
