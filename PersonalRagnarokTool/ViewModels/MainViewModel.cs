using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Media;
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
    private readonly ClientPreviewService _previewService;
    private readonly GlobalHotkeyService _hotkeyService;
    private readonly MacroExecutor _macroExecutor;
    private readonly AppConfig _config;
    private ClientProfile? _selectedClient;
    private MacroBinding? _selectedBinding;
    private TraceSequence? _selectedTrace;
    private NormalizedPoint? _selectedTracePoint;
    private ClientWindowRef? _selectedAvailableWindow;
    private ImageSource? _previewImage;
    private double _previewWidth = 960;
    private double _previewHeight = 540;
    private string _statusMessage = "Ready.";
    private string _previewStatus = "Bind a client to load its preview.";
    private string _hotkeyStatus = "Hotkeys are waiting for registration.";
    private string _selectedClientBindingStatus = "Unbound";
    private string _selectedClientBindingDetail = "Bind this profile to a live client window.";
    private string _newTraceName = string.Empty;
    private string _selectedBindingPostInputDelayText = string.Empty;
    private string _selectedBindingInterClickDelayText = string.Empty;
    private string _bindingEditorValidationMessage = string.Empty;
    private bool _isSidebarCollapsed;
    private bool _isUpdatingBindingEditorFields;

    public MainViewModel(string configPath, AppConfigStore configStore, ClientDiscoveryService discoveryService, ClientBindingService bindingService, ClientPreviewService previewService, GlobalHotkeyService hotkeyService, MacroExecutor macroExecutor)
    {
        _configPath = configPath;
        _configStore = configStore;
        _discoveryService = discoveryService;
        _bindingService = bindingService;
        _previewService = previewService;
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
        RefreshPreviewCommand = new RelayCommand(() => _ = RefreshPreviewAsync(), () => SelectedClient is not null);
        AddTraceCommand = new RelayCommand(AddTrace, () => SelectedClient is not null);
        RemoveTraceCommand = new RelayCommand(RemoveSelectedTrace, () => SelectedTrace is not null);
        RemoveTracePointCommand = new RelayCommand(RemoveSelectedTracePoint, () => SelectedTracePoint is not null);
        MoveTracePointUpCommand = new RelayCommand(() => MoveSelectedTracePoint(-1), () => CanMoveSelectedTracePoint(-1));
        MoveTracePointDownCommand = new RelayCommand(() => MoveSelectedTracePoint(1), () => CanMoveSelectedTracePoint(1));
        NudgeTracePointUpCommand = new RelayCommand(() => NudgeSelectedTracePoint(0d, -0.01d), () => SelectedTracePoint is not null);
        NudgeTracePointDownCommand = new RelayCommand(() => NudgeSelectedTracePoint(0d, 0.01d), () => SelectedTracePoint is not null);
        NudgeTracePointLeftCommand = new RelayCommand(() => NudgeSelectedTracePoint(-0.01d, 0d), () => SelectedTracePoint is not null);
        NudgeTracePointRightCommand = new RelayCommand(() => NudgeSelectedTracePoint(0.01d, 0d), () => SelectedTracePoint is not null);
        ClearTracePointsCommand = new RelayCommand(ClearTracePoints, () => SelectedTrace?.Points.Count > 0);
        AddBindingCommand = new RelayCommand(AddBinding, () => SelectedClient is not null);
        RemoveBindingCommand = new RelayCommand(RemoveSelectedBinding, () => SelectedBinding is not null);
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);
        AttachConfigSubscriptions();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        RefreshWindows();
        SelectedClient = _config.ClientProfiles.FirstOrDefault();
        if (SelectedClient is null) AddClient();
    }

    public event EventHandler? TracePointsChanged;
    public ObservableCollection<ClientProfile> ClientProfiles => _config.ClientProfiles;
    public ObservableCollection<ClientWindowRef> AvailableWindows { get; }
    public InputMethod[] InputMethods => Enum.GetValues<InputMethod>();
    public InputMethod SelectedInputMethod { get => _config.InputMethod; set { if (_config.InputMethod != value) { _config.InputMethod = value; RaisePropertyChanged(); SaveConfig(); } } }
    public RelayCommand AddClientCommand { get; }
    public RelayCommand RemoveSelectedClientCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand RefreshWindowsCommand { get; }
    public RelayCommand BindSelectedClientCommand { get; }
    public RelayCommand UnbindSelectedClientCommand { get; }
    public RelayCommand ResolveSelectedClientCommand { get; }
    public RelayCommand RefreshPreviewCommand { get; }
    public RelayCommand AddTraceCommand { get; }
    public RelayCommand RemoveTraceCommand { get; }
    public RelayCommand RemoveTracePointCommand { get; }
    public RelayCommand MoveTracePointUpCommand { get; }
    public RelayCommand MoveTracePointDownCommand { get; }
    public RelayCommand NudgeTracePointUpCommand { get; }
    public RelayCommand NudgeTracePointDownCommand { get; }
    public RelayCommand NudgeTracePointLeftCommand { get; }
    public RelayCommand NudgeTracePointRightCommand { get; }
    public RelayCommand ClearTracePointsCommand { get; }
    public RelayCommand AddBindingCommand { get; }
    public RelayCommand RemoveBindingCommand { get; }
    public RelayCommand ToggleSidebarCommand { get; }
    public ClientProfile? SelectedClient { get => _selectedClient; set { if (SetProperty(ref _selectedClient, value)) { SelectedBinding = value?.Bindings.FirstOrDefault(); SelectedTrace = value?.TraceSequences.FirstOrDefault(); SelectedAvailableWindow = FindMatchingAvailableWindow(value?.BoundWindow); RefreshSelectedClientRuntimeState(true); RefreshCommandStates(); TracePointsChanged?.Invoke(this, EventArgs.Empty); _ = RefreshPreviewAsync(); } } }
    public MacroBinding? SelectedBinding { get => _selectedBinding; set { if (SetProperty(ref _selectedBinding, value)) { RefreshBindingEditorFields(); if (value is not null && SelectedClient is not null) SelectedTrace = ResolveBindingSequence(SelectedClient, value); RefreshCommandStates(); } } }
    public TraceSequence? SelectedTrace { get => _selectedTrace; set { if (SetProperty(ref _selectedTrace, value)) { SelectedTracePoint = value?.Points.FirstOrDefault(); if (_selectedBinding is not null) _selectedBinding.TraceSequenceId = value?.Id; RefreshCommandStates(); TracePointsChanged?.Invoke(this, EventArgs.Empty); } } }
    public NormalizedPoint? SelectedTracePoint
    {
        get => _selectedTracePoint;
        set
        {
            if (ReferenceEquals(_selectedTracePoint, value)) return;
            if (_selectedTracePoint is not null) _selectedTracePoint.PropertyChanged -= OnSelectedTracePointPropertyChanged;
            _selectedTracePoint = value;
            if (_selectedTracePoint is not null) _selectedTracePoint.PropertyChanged += OnSelectedTracePointPropertyChanged;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SelectedTracePointSummary));
            RefreshCommandStates();
            TracePointsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public ClientWindowRef? SelectedAvailableWindow { get => _selectedAvailableWindow; set { if (SetProperty(ref _selectedAvailableWindow, value)) RefreshCommandStates(); } }
    public ImageSource? PreviewImage { get => _previewImage; private set => SetProperty(ref _previewImage, value); }
    public double PreviewWidth { get => _previewWidth; private set => SetProperty(ref _previewWidth, Math.Max(320, value)); }
    public double PreviewHeight { get => _previewHeight; private set => SetProperty(ref _previewHeight, Math.Max(240, value)); }
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
    public string PreviewStatus { get => _previewStatus; private set => SetProperty(ref _previewStatus, value); }
    public string HotkeyStatus { get => _hotkeyStatus; private set => SetProperty(ref _hotkeyStatus, value); }
    public string SelectedClientBindingStatus { get => _selectedClientBindingStatus; private set => SetProperty(ref _selectedClientBindingStatus, value); }
    public string SelectedClientBindingDetail { get => _selectedClientBindingDetail; private set => SetProperty(ref _selectedClientBindingDetail, value); }
    public string NewTraceName { get => _newTraceName; set => SetProperty(ref _newTraceName, value); }
    public string SelectedBindingPostInputDelayText { get => _selectedBindingPostInputDelayText; set { if (SetProperty(ref _selectedBindingPostInputDelayText, value)) ApplyBindingNumericFieldChanges(); } }
    public string SelectedBindingInterClickDelayText { get => _selectedBindingInterClickDelayText; set { if (SetProperty(ref _selectedBindingInterClickDelayText, value)) ApplyBindingNumericFieldChanges(); } }
    public string BindingEditorValidationMessage { get => _bindingEditorValidationMessage; private set => SetProperty(ref _bindingEditorValidationMessage, value); }
    public bool IsSidebarCollapsed { get => _isSidebarCollapsed; set { if (SetProperty(ref _isSidebarCollapsed, value)) { RaisePropertyChanged(nameof(SidebarWidth)); RaisePropertyChanged(nameof(SidebarToggleLabel)); } } }
    public double SidebarWidth => IsSidebarCollapsed ? 48 : 244;
    public string SidebarToggleLabel => IsSidebarCollapsed ? ">>" : "<<";
    public string SelectedTracePointSummary => SelectedTracePoint is null ? "No sequence point selected." : $"X {SelectedTracePoint.X:0.000}, Y {SelectedTracePoint.Y:0.000}";

    public void AttachWindowHandle(IntPtr windowHandle) { _hotkeyService.SetWindowHandle(windowHandle); RefreshHotkeys(); }
    public void AddSequencePoint(double previewX, double previewY) { if (SelectedTrace is null) { StatusMessage = "Create or select a click sequence first."; return; } var point = CoordinateTranslator.ToNormalized((int)Math.Round(previewX), (int)Math.Round(previewY), (int)PreviewWidth, (int)PreviewHeight); SelectedTrace.Points.Add(point); SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; SelectedTracePoint = point; StatusMessage = $"{SelectedTrace.Name}: point {SelectedTrace.Points.Count} added."; RefreshCommandStates(); TracePointsChanged?.Invoke(this, EventArgs.Empty); }
    public void MoveSelectedTracePointOnPreview(double previewX, double previewY) { if (SelectedTracePoint is null || SelectedTrace is null) return; var point = CoordinateTranslator.ToNormalized((int)Math.Round(previewX), (int)Math.Round(previewY), (int)PreviewWidth, (int)PreviewHeight); SelectedTracePoint.X = point.X; SelectedTracePoint.Y = point.Y; SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; RaisePropertyChanged(nameof(SelectedTracePointSummary)); TracePointsChanged?.Invoke(this, EventArgs.Empty); }

    private void RefreshBindingEditorFields()
    {
        _isUpdatingBindingEditorFields = true;
        SelectedBindingPostInputDelayText = SelectedBinding?.PostInputDelayMs.ToString() ?? string.Empty;
        SelectedBindingInterClickDelayText = SelectedBinding?.InterClickDelayMs.ToString() ?? string.Empty;
        BindingEditorValidationMessage = string.Empty;
        _isUpdatingBindingEditorFields = false;
    }

    private void ApplyBindingNumericFieldChanges()
    {
        if (_isUpdatingBindingEditorFields || SelectedBinding is null) return;
        var issues = new List<string>();
        if (TryParseRequiredNonNegativeInt(SelectedBindingPostInputDelayText, out int postDelay)) SelectedBinding.PostInputDelayMs = postDelay; else issues.Add("Post-input delay must be a non-negative integer.");
        if (TryParseRequiredNonNegativeInt(SelectedBindingInterClickDelayText, out int interDelay)) SelectedBinding.InterClickDelayMs = interDelay; else issues.Add("Inter-click delay must be a non-negative integer.");
        BindingEditorValidationMessage = string.Join(" ", issues);
    }

    private static bool TryParseRequiredNonNegativeInt(string value, out int parsedValue)
    {
        parsedValue = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsedValue) && parsedValue >= 0;
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
    private void AddClient() { int number = ClientProfiles.Count + 1; var profile = new ClientProfile { DisplayName = $"Client {number}" }; profile.TraceSequences.Add(new TraceSequence { Name = "Sequence 1" }); profile.Bindings.Add(new MacroBinding { ClientProfileId = profile.Id, Name = $"{profile.DisplayName} Binding 1" }); ClientProfiles.Add(profile); SelectedClient = profile; StatusMessage = $"{profile.DisplayName} added."; }
    private void RemoveSelectedClient() { if (SelectedClient is null) return; int index = ClientProfiles.IndexOf(SelectedClient); string displayName = SelectedClient.DisplayName; ClientProfiles.Remove(SelectedClient); SelectedClient = ClientProfiles.Count == 0 ? null : ClientProfiles[Math.Clamp(index - 1, 0, ClientProfiles.Count - 1)]; StatusMessage = $"{displayName} removed."; }
    private void SaveConfig() { _configStore.Save(_configPath, _config); RefreshAllClientRuntimeStates(false); RefreshHotkeys(); StatusMessage = $"Configuration saved to {_configPath}."; }
    private void RefreshWindows() { AvailableWindows.Clear(); foreach (var window in _discoveryService.DiscoverWindows()) AvailableWindows.Add(window); RefreshAllClientRuntimeStates(true); SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient?.BoundWindow) ?? AvailableWindows.FirstOrDefault(); StatusMessage = AvailableWindows.Count == 0 ? "No windows discovered." : $"Discovered {AvailableWindows.Count} window(s). Selected client status: {SelectedClientBindingStatus}."; }
    private void BindSelectedClient() { if (SelectedClient is null || SelectedAvailableWindow is null) return; _bindingService.BindProfile(SelectedClient, SelectedAvailableWindow); RefreshSelectedClientRuntimeState(true); SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient.BoundWindow); StatusMessage = $"{SelectedClient.DisplayName} bound to {SelectedClient.BoundWindowDisplayText}."; _ = RefreshPreviewAsync(); }
    private void UnbindSelectedClient() { if (SelectedClient is null) return; _bindingService.ClearBinding(SelectedClient); RefreshSelectedClientRuntimeState(false); StatusMessage = $"{SelectedClient.DisplayName} unbound from any client window."; _ = RefreshPreviewAsync(); }
    private void ResolveSelectedClient() { if (SelectedClient is null) return; RefreshSelectedClientRuntimeState(true); StatusMessage = $"{SelectedClient.DisplayName}: {SelectedClientBindingDetail}"; _ = RefreshPreviewAsync(); }

    private async Task RefreshPreviewAsync()
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (SelectedClient is null) { PreviewImage = null; PreviewWidth = 960; PreviewHeight = 540; PreviewStatus = "Select a client."; TracePointsChanged?.Invoke(this, EventArgs.Empty); return; }
            var snapshot = _previewService.Capture(SelectedClient);
            PreviewImage = snapshot.Image;
            PreviewWidth = snapshot.ClientWidth;
            PreviewHeight = snapshot.ClientHeight;
            PreviewStatus = snapshot.Status;
            RefreshSelectedClientRuntimeState(true);
            TracePointsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void AddTrace() { if (SelectedClient is null) return; var trace = new TraceSequence { Name = string.IsNullOrWhiteSpace(NewTraceName) ? $"Sequence {SelectedClient.TraceSequences.Count + 1}" : NewTraceName.Trim() }; SelectedClient.TraceSequences.Add(trace); SelectedTrace = trace; NewTraceName = string.Empty; EnsureBindingSequenceSelection(); StatusMessage = $"{SelectedClient.DisplayName}: sequence '{trace.Name}' added."; }
    private void RemoveSelectedTrace() { if (SelectedClient is null || SelectedTrace is null) return; string name = SelectedTrace.Name; string? removedId = SelectedTrace.Id; SelectedClient.TraceSequences.Remove(SelectedTrace); foreach (var binding in SelectedClient.Bindings.Where(x => x.TraceSequenceId == removedId)) binding.TraceSequenceId = SelectedClient.TraceSequences.FirstOrDefault()?.Id; SelectedTrace = SelectedClient.TraceSequences.FirstOrDefault(); EnsureBindingSequenceSelection(); StatusMessage = $"{SelectedClient.DisplayName}: sequence '{name}' removed."; }
    private void RemoveSelectedTracePoint() { if (SelectedTrace is null || SelectedTracePoint is null) return; int index = SelectedTrace.Points.IndexOf(SelectedTracePoint); if (index < 0) return; SelectedTrace.Points.RemoveAt(index); SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; SelectedTracePoint = SelectedTrace.Points.ElementAtOrDefault(Math.Clamp(index, 0, SelectedTrace.Points.Count - 1)); StatusMessage = $"{SelectedTrace.Name}: point removed."; RefreshCommandStates(); TracePointsChanged?.Invoke(this, EventArgs.Empty); }
    private void ClearTracePoints() { if (SelectedTrace is null || SelectedTrace.Points.Count == 0) return; SelectedTrace.Points.Clear(); SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; SelectedTracePoint = null; StatusMessage = $"{SelectedTrace.Name}: all points cleared."; RefreshCommandStates(); TracePointsChanged?.Invoke(this, EventArgs.Empty); }
    private bool CanMoveSelectedTracePoint(int offset) { if (SelectedTrace is null || SelectedTracePoint is null) return false; int index = SelectedTrace.Points.IndexOf(SelectedTracePoint); if (index < 0) return false; int targetIndex = index + offset; return targetIndex >= 0 && targetIndex < SelectedTrace.Points.Count; }
    private void MoveSelectedTracePoint(int offset) { if (!CanMoveSelectedTracePoint(offset) || SelectedTrace is null || SelectedTracePoint is null) return; int index = SelectedTrace.Points.IndexOf(SelectedTracePoint); int targetIndex = index + offset; SelectedTrace.Points.Move(index, targetIndex); SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; SelectedTracePoint = SelectedTrace.Points[targetIndex]; StatusMessage = $"{SelectedTrace.Name}: point order updated."; TracePointsChanged?.Invoke(this, EventArgs.Empty); }
    private void NudgeSelectedTracePoint(double deltaX, double deltaY) { if (SelectedTracePoint is null || SelectedTrace is null) return; SelectedTracePoint.X = Math.Clamp(SelectedTracePoint.X + deltaX, 0d, 1d); SelectedTracePoint.Y = Math.Clamp(SelectedTracePoint.Y + deltaY, 0d, 1d); SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; RaisePropertyChanged(nameof(SelectedTracePointSummary)); StatusMessage = $"{SelectedTrace.Name}: selected point nudged."; TracePointsChanged?.Invoke(this, EventArgs.Empty); }
    private void AddBinding() { if (SelectedClient is null) return; var binding = new MacroBinding { ClientProfileId = SelectedClient.Id, Name = $"{SelectedClient.DisplayName} Binding {SelectedClient.Bindings.Count + 1}", TraceSequenceId = SelectedClient.TraceSequences.FirstOrDefault()?.Id }; SelectedClient.Bindings.Add(binding); SelectedBinding = binding; StatusMessage = $"{SelectedClient.DisplayName}: binding '{binding.Name}' added."; }
    private void RemoveSelectedBinding() { if (SelectedClient is null || SelectedBinding is null) return; string name = SelectedBinding.Name; SelectedClient.Bindings.Remove(SelectedBinding); SelectedBinding = SelectedClient.Bindings.FirstOrDefault(); if (SelectedBinding is not null) SelectedTrace = ResolveBindingSequence(SelectedClient, SelectedBinding); StatusMessage = $"{SelectedClient.DisplayName}: binding '{name}' removed."; }

    private void AttachConfigSubscriptions() { ClientProfiles.CollectionChanged += OnProfilesCollectionChanged; foreach (var profile in ClientProfiles) AttachProfileSubscriptions(profile); }
    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { if (e.NewItems is not null) foreach (ClientProfile profile in e.NewItems) AttachProfileSubscriptions(profile); if (e.OldItems is not null) foreach (ClientProfile profile in e.OldItems) DetachProfileSubscriptions(profile); BindingValidator.NormalizeConfig(_config); RefreshHotkeys(); RefreshCommandStates(); }
    private void AttachProfileSubscriptions(ClientProfile profile) { profile.PropertyChanged += OnProfilePropertyChanged; profile.Bindings.CollectionChanged += OnBindingsCollectionChanged; profile.TraceSequences.CollectionChanged += OnTraceSequencesCollectionChanged; foreach (var binding in profile.Bindings) binding.PropertyChanged += OnBindingPropertyChanged; }
    private void DetachProfileSubscriptions(ClientProfile profile) { profile.PropertyChanged -= OnProfilePropertyChanged; profile.Bindings.CollectionChanged -= OnBindingsCollectionChanged; profile.TraceSequences.CollectionChanged -= OnTraceSequencesCollectionChanged; foreach (var binding in profile.Bindings) binding.PropertyChanged -= OnBindingPropertyChanged; }
    private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(ClientProfile.IsEnabled) or nameof(ClientProfile.BoundWindow)) RefreshHotkeys(); }
    private void OnBindingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { if (e.NewItems is not null) foreach (MacroBinding binding in e.NewItems) binding.PropertyChanged += OnBindingPropertyChanged; if (e.OldItems is not null) foreach (MacroBinding binding in e.OldItems) binding.PropertyChanged -= OnBindingPropertyChanged; BindingValidator.NormalizeConfig(_config); RefreshHotkeys(); RefreshCommandStates(); }
    private void OnTraceSequencesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { EnsureBindingSequenceSelection(); RefreshCommandStates(); }
    private void OnBindingPropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(MacroBinding.TriggerHotkey) or nameof(MacroBinding.IsEnabled)) RefreshHotkeys(); else if (e.PropertyName == nameof(MacroBinding.TraceSequenceId) && sender is MacroBinding binding && SelectedClient is not null && ReferenceEquals(binding, SelectedBinding)) SelectedTrace = ResolveBindingSequence(SelectedClient, binding); }

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
    private void OnSelectedTracePointPropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName is nameof(NormalizedPoint.X) or nameof(NormalizedPoint.Y)) { if (SelectedTrace is not null) SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow; RaisePropertyChanged(nameof(SelectedTracePointSummary)); } }
    private ClientWindowRef? FindMatchingAvailableWindow(ClientWindowRef? boundWindow) => boundWindow is null ? null : AvailableWindows.FirstOrDefault(window => window.WindowHandle == boundWindow.WindowHandle) ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId && string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase)) ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId);
    private static TraceSequence? ResolveBindingSequence(ClientProfile profile, MacroBinding binding) => profile.TraceSequences.FirstOrDefault(x => x.Id == binding.TraceSequenceId) ?? profile.TraceSequences.FirstOrDefault();
    private void EnsureBindingSequenceSelection() { if (SelectedClient is null) return; string? fallbackId = SelectedClient.TraceSequences.FirstOrDefault()?.Id; foreach (var binding in SelectedClient.Bindings) if (binding.TraceSequenceId is null || SelectedClient.TraceSequences.All(x => x.Id != binding.TraceSequenceId)) binding.TraceSequenceId = fallbackId; }
    private void RefreshCommandStates() { RemoveSelectedClientCommand.RaiseCanExecuteChanged(); BindSelectedClientCommand.RaiseCanExecuteChanged(); UnbindSelectedClientCommand.RaiseCanExecuteChanged(); ResolveSelectedClientCommand.RaiseCanExecuteChanged(); RefreshPreviewCommand.RaiseCanExecuteChanged(); AddTraceCommand.RaiseCanExecuteChanged(); RemoveTraceCommand.RaiseCanExecuteChanged(); RemoveTracePointCommand.RaiseCanExecuteChanged(); MoveTracePointUpCommand.RaiseCanExecuteChanged(); MoveTracePointDownCommand.RaiseCanExecuteChanged(); NudgeTracePointUpCommand.RaiseCanExecuteChanged(); NudgeTracePointDownCommand.RaiseCanExecuteChanged(); NudgeTracePointLeftCommand.RaiseCanExecuteChanged(); NudgeTracePointRightCommand.RaiseCanExecuteChanged(); ClearTracePointsCommand.RaiseCanExecuteChanged(); AddBindingCommand.RaiseCanExecuteChanged(); RemoveBindingCommand.RaiseCanExecuteChanged(); }
    public void Dispose() { SaveConfig(); _hotkeyService.HotkeyPressed -= OnHotkeyPressed; _hotkeyService.UnregisterAll(); }
}
