using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
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
    private readonly TraceRecorder _traceRecorder;
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
    private string _selectedBindingClickCountOverrideText = string.Empty;
    private string _bindingEditorValidationMessage = string.Empty;
    private bool _isSidebarCollapsed;
    private int? _selectedVertexIndex;
    private bool _isTraceRecording;
    private bool _isUpdatingBindingEditorFields;

    public MainViewModel(
        string configPath,
        AppConfigStore configStore,
        ClientDiscoveryService discoveryService,
        ClientBindingService bindingService,
        ClientPreviewService previewService,
        GlobalHotkeyService hotkeyService,
        TraceRecorder traceRecorder,
        MacroExecutor macroExecutor)
    {
        _configPath = configPath;
        _configStore = configStore;
        _discoveryService = discoveryService;
        _bindingService = bindingService;
        _previewService = previewService;
        _hotkeyService = hotkeyService;
        _traceRecorder = traceRecorder;
        _macroExecutor = macroExecutor;
        _config = _configStore.Load(_configPath);

        AvailableWindows = new ObservableCollection<ClientWindowRef>();
        ExecutionModes = Enum.GetValues<ExecutionMode>();

        AddClientCommand = new RelayCommand(AddClient);
        RemoveSelectedClientCommand = new RelayCommand(RemoveSelectedClient, () => SelectedClient is not null);
        SaveCommand = new RelayCommand(SaveConfig);
        RefreshWindowsCommand = new RelayCommand(RefreshWindows);
        BindSelectedClientCommand = new RelayCommand(BindSelectedClient, () => SelectedClient is not null && SelectedAvailableWindow is not null);
        UnbindSelectedClientCommand = new RelayCommand(UnbindSelectedClient, () => SelectedClient?.BoundWindow is not null);
        ResolveSelectedClientCommand = new RelayCommand(ResolveSelectedClient, () => SelectedClient?.BoundWindow is not null);
        RefreshPreviewCommand = new RelayCommand(() => _ = RefreshPreviewAsync(), () => SelectedClient is not null);
        NewPolygonCommand = new RelayCommand(StartNewPolygon, () => SelectedClient is not null);
        ClosePolygonCommand = new RelayCommand(ClosePolygon, () => SelectedClient?.ActionPolygon.Vertices.Count >= 3);
        DeleteSelectedVertexCommand = new RelayCommand(DeleteSelectedVertex, () => SelectedClient is not null && SelectedVertexIndex is not null);
        ClearPolygonCommand = new RelayCommand(ClearPolygon, () => SelectedClient is not null && SelectedClient.ActionPolygon.Vertices.Count > 0);
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
        StartTraceCommand = new RelayCommand(StartTraceRecording, () => SelectedClient is not null && SelectedTrace is not null && !IsTraceRecording);
        StopTraceCommand = new RelayCommand(StopTraceRecording, () => IsTraceRecording);
        AddBindingCommand = new RelayCommand(AddBinding, () => SelectedClient is not null);
        RemoveBindingCommand = new RelayCommand(RemoveSelectedBinding, () => SelectedBinding is not null);
        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

        AttachConfigSubscriptions();
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _traceRecorder.PointCaptured += OnTracePointCaptured;

        RefreshWindows();
        if (_config.ClientProfiles.Count == 0)
        {
            AddClient();
        }
        else
        {
            SelectedClient = _config.ClientProfiles[0];
        }
    }

    public event EventHandler? PolygonChanged;
    public event EventHandler? TracePointsChanged;

    public ObservableCollection<ClientProfile> ClientProfiles => _config.ClientProfiles;

    public ObservableCollection<ClientWindowRef> AvailableWindows { get; }

    public Array ExecutionModes { get; }

    public RelayCommand AddClientCommand { get; }

    public RelayCommand RemoveSelectedClientCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand RefreshWindowsCommand { get; }

    public RelayCommand BindSelectedClientCommand { get; }

    public RelayCommand UnbindSelectedClientCommand { get; }

    public RelayCommand ResolveSelectedClientCommand { get; }

    public RelayCommand RefreshPreviewCommand { get; }

    public RelayCommand NewPolygonCommand { get; }

    public RelayCommand ClosePolygonCommand { get; }

    public RelayCommand DeleteSelectedVertexCommand { get; }

    public RelayCommand ClearPolygonCommand { get; }

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

    public RelayCommand StartTraceCommand { get; }

    public RelayCommand StopTraceCommand { get; }

    public RelayCommand AddBindingCommand { get; }

    public RelayCommand RemoveBindingCommand { get; }

    public RelayCommand ToggleSidebarCommand { get; }

    public ClientProfile? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (!ReferenceEquals(_selectedClient, value) && IsTraceRecording)
            {
                StopTraceRecording();
            }

            if (SetProperty(ref _selectedClient, value))
            {
                SelectedBinding = value?.Bindings.FirstOrDefault();
                SelectedTrace = value?.TraceSequences.FirstOrDefault();
                SelectedAvailableWindow = FindMatchingAvailableWindow(value?.BoundWindow);
                SelectedVertexIndex = null;
                RefreshSelectedClientRuntimeState(allowAutoRebind: true);
                RefreshCommandStates();
                PolygonChanged?.Invoke(this, EventArgs.Empty);
                _ = RefreshPreviewAsync();
            }
        }
    }

    public MacroBinding? SelectedBinding
    {
        get => _selectedBinding;
        set
        {
            if (SetProperty(ref _selectedBinding, value))
            {
                RefreshBindingEditorFields();
                RefreshCommandStates();
            }
        }
    }

    public TraceSequence? SelectedTrace
    {
        get => _selectedTrace;
        set
        {
            if (!ReferenceEquals(_selectedTrace, value) && IsTraceRecording)
            {
                StopTraceRecording();
            }

            if (SetProperty(ref _selectedTrace, value))
            {
                SelectedTracePoint = value?.Points.FirstOrDefault();
                RefreshCommandStates();
            }
        }
    }

    public NormalizedPoint? SelectedTracePoint
    {
        get => _selectedTracePoint;
        set
        {
            if (ReferenceEquals(_selectedTracePoint, value))
            {
                return;
            }

            if (_selectedTracePoint is not null)
            {
                _selectedTracePoint.PropertyChanged -= OnSelectedTracePointPropertyChanged;
            }

            _selectedTracePoint = value;

            if (_selectedTracePoint is not null)
            {
                _selectedTracePoint.PropertyChanged += OnSelectedTracePointPropertyChanged;
            }

            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SelectedTracePointSummary));
            RefreshCommandStates();
        }
    }

    public ClientWindowRef? SelectedAvailableWindow
    {
        get => _selectedAvailableWindow;
        set
        {
            if (SetProperty(ref _selectedAvailableWindow, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set => SetProperty(ref _previewImage, value);
    }

    public double PreviewWidth
    {
        get => _previewWidth;
        private set => SetProperty(ref _previewWidth, Math.Max(320, value));
    }

    public double PreviewHeight
    {
        get => _previewHeight;
        private set => SetProperty(ref _previewHeight, Math.Max(240, value));
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PreviewStatus
    {
        get => _previewStatus;
        private set => SetProperty(ref _previewStatus, value);
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

    public string NewTraceName
    {
        get => _newTraceName;
        set => SetProperty(ref _newTraceName, value);
    }

    public string SelectedBindingPostInputDelayText
    {
        get => _selectedBindingPostInputDelayText;
        set
        {
            if (SetProperty(ref _selectedBindingPostInputDelayText, value))
            {
                ApplyBindingNumericFieldChanges();
            }
        }
    }

    public string SelectedBindingInterClickDelayText
    {
        get => _selectedBindingInterClickDelayText;
        set
        {
            if (SetProperty(ref _selectedBindingInterClickDelayText, value))
            {
                ApplyBindingNumericFieldChanges();
            }
        }
    }

    public string SelectedBindingClickCountOverrideText
    {
        get => _selectedBindingClickCountOverrideText;
        set
        {
            if (SetProperty(ref _selectedBindingClickCountOverrideText, value))
            {
                ApplyBindingNumericFieldChanges();
            }
        }
    }

    public string BindingEditorValidationMessage
    {
        get => _bindingEditorValidationMessage;
        private set => SetProperty(ref _bindingEditorValidationMessage, value);
    }

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

    public double SidebarWidth => IsSidebarCollapsed ? 48 : 244;

    public string SidebarToggleLabel => IsSidebarCollapsed ? ">>" : "<<";

    public int? SelectedVertexIndex
    {
        get => _selectedVertexIndex;
        set
        {
            if (SetProperty(ref _selectedVertexIndex, value))
            {
                RefreshCommandStates();
                PolygonChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsTraceRecording
    {
        get => _isTraceRecording;
        private set
        {
            if (SetProperty(ref _isTraceRecording, value))
            {
                RefreshCommandStates();
            }
        }
    }

    public string SelectedTracePointSummary =>
        SelectedTracePoint is null
            ? "No trace point selected."
            : $"X {SelectedTracePoint.X:0.000}, Y {SelectedTracePoint.Y:0.000}";

    public void AttachWindowHandle(IntPtr windowHandle)
    {
        _hotkeyService.SetWindowHandle(windowHandle);
        RefreshHotkeys();
    }

    private void RefreshBindingEditorFields()
    {
        _isUpdatingBindingEditorFields = true;
        try
        {
            SelectedBindingPostInputDelayText = SelectedBinding?.PostInputDelayMs.ToString() ?? string.Empty;
            SelectedBindingInterClickDelayText = SelectedBinding?.InterClickDelayMs.ToString() ?? string.Empty;
            SelectedBindingClickCountOverrideText = SelectedBinding?.ClickCountOverride?.ToString() ?? string.Empty;
            BindingEditorValidationMessage = string.Empty;
        }
        finally
        {
            _isUpdatingBindingEditorFields = false;
        }
    }

    private void ApplyBindingNumericFieldChanges()
    {
        if (_isUpdatingBindingEditorFields || SelectedBinding is null)
        {
            return;
        }

        var issues = new List<string>();

        if (TryParseRequiredNonNegativeInt(SelectedBindingPostInputDelayText, out int postInputDelay))
        {
            SelectedBinding.PostInputDelayMs = postInputDelay;
        }
        else
        {
            issues.Add("Post-input delay must be a non-negative integer.");
        }

        if (TryParseRequiredNonNegativeInt(SelectedBindingInterClickDelayText, out int interClickDelay))
        {
            SelectedBinding.InterClickDelayMs = interClickDelay;
        }
        else
        {
            issues.Add("Inter-click delay must be a non-negative integer.");
        }

        if (string.IsNullOrWhiteSpace(SelectedBindingClickCountOverrideText))
        {
            SelectedBinding.ClickCountOverride = null;
        }
        else if (int.TryParse(SelectedBindingClickCountOverrideText, out int clickCount) && clickCount > 0)
        {
            SelectedBinding.ClickCountOverride = clickCount;
        }
        else
        {
            issues.Add("Click count override must be blank or greater than zero.");
        }

        BindingEditorValidationMessage = string.Join(" ", issues);
    }

    private static bool TryParseRequiredNonNegativeInt(string value, out int parsedValue)
    {
        parsedValue = 0;
        return !string.IsNullOrWhiteSpace(value) && int.TryParse(value, out parsedValue) && parsedValue >= 0;
    }

    private void RefreshSelectedClientRuntimeState(bool allowAutoRebind)
    {
        if (SelectedClient is null)
        {
            SelectedClientBindingStatus = "Unbound";
            SelectedClientBindingDetail = "Select a client profile.";
            return;
        }

        var resolution = _bindingService.GetResolution(SelectedClient, AvailableWindows.ToArray(), allowAutoRebind);
        SelectedClientBindingStatus = resolution.Label;
        SelectedClientBindingDetail = resolution.Detail;
        SelectedAvailableWindow = resolution.ResolvedWindow
            ?? FindMatchingAvailableWindow(SelectedClient.BoundWindow)
            ?? SelectedAvailableWindow;
    }

    private void RefreshAllClientRuntimeStates(bool allowAutoRebind)
    {
        var liveWindows = AvailableWindows.ToArray();
        foreach (var profile in ClientProfiles)
        {
            _bindingService.GetResolution(profile, liveWindows, allowAutoRebind);
        }

        RefreshSelectedClientRuntimeState(allowAutoRebind);
    }

    public void AddPolygonVertex(double previewX, double previewY)
    {
        if (SelectedClient is null)
        {
            return;
        }

        var point = CoordinateTranslator.ToNormalized((int)Math.Round(previewX), (int)Math.Round(previewY), (int)PreviewWidth, (int)PreviewHeight);
        SelectedClient.ActionPolygon.Vertices.Add(point);
        SelectedVertexIndex = SelectedClient.ActionPolygon.Vertices.Count - 1;
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    public void MovePolygonVertex(int vertexIndex, double previewX, double previewY)
    {
        if (SelectedClient is null || vertexIndex < 0 || vertexIndex >= SelectedClient.ActionPolygon.Vertices.Count)
        {
            return;
        }

        var point = CoordinateTranslator.ToNormalized((int)Math.Round(previewX), (int)Math.Round(previewY), (int)PreviewWidth, (int)PreviewHeight);
        SelectedClient.ActionPolygon.Vertices[vertexIndex].X = point.X;
        SelectedClient.ActionPolygon.Vertices[vertexIndex].Y = point.Y;
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddClient()
    {
        int number = ClientProfiles.Count + 1;
        var profile = new ClientProfile
        {
            DisplayName = $"Client {number}",
        };

        ClientProfiles.Add(profile);
        SelectedClient = profile;
        StatusMessage = $"{profile.DisplayName} added.";
    }

    private void RemoveSelectedClient()
    {
        if (SelectedClient is null)
        {
            return;
        }

        int index = ClientProfiles.IndexOf(SelectedClient);
        string displayName = SelectedClient.DisplayName;
        ClientProfiles.Remove(SelectedClient);
        SelectedClient = ClientProfiles.Count == 0 ? null : ClientProfiles[Math.Clamp(index - 1, 0, ClientProfiles.Count - 1)];
        StatusMessage = $"{displayName} removed.";
    }

    private void SaveConfig()
    {
        _configStore.Save(_configPath, _config);
        RefreshAllClientRuntimeStates(allowAutoRebind: false);
        RefreshHotkeys();
        StatusMessage = $"Configuration saved to {_configPath}.";
    }

    private void RefreshWindows()
    {
        AvailableWindows.Clear();
        foreach (var window in _discoveryService.DiscoverWindows())
        {
            AvailableWindows.Add(window);
        }

        RefreshAllClientRuntimeStates(allowAutoRebind: true);
        SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient?.BoundWindow) ?? AvailableWindows.FirstOrDefault();
        StatusMessage = AvailableWindows.Count == 0
            ? "No windows discovered."
            : $"Discovered {AvailableWindows.Count} window(s). Selected client status: {SelectedClientBindingStatus}.";
    }

    private void BindSelectedClient()
    {
        if (SelectedClient is null || SelectedAvailableWindow is null)
        {
            return;
        }

        _bindingService.BindProfile(SelectedClient, SelectedAvailableWindow);
        RefreshSelectedClientRuntimeState(allowAutoRebind: true);
        SelectedAvailableWindow = FindMatchingAvailableWindow(SelectedClient.BoundWindow);
        StatusMessage = $"{SelectedClient.DisplayName} bound to {SelectedClient.BoundWindowDisplayText}.";
        _ = RefreshPreviewAsync();
    }

    private void UnbindSelectedClient()
    {
        if (SelectedClient is null)
        {
            return;
        }

        _bindingService.ClearBinding(SelectedClient);
        RefreshSelectedClientRuntimeState(allowAutoRebind: false);
        StatusMessage = $"{SelectedClient.DisplayName} unbound from any client window.";
        _ = RefreshPreviewAsync();
    }

    private void ResolveSelectedClient()
    {
        if (SelectedClient is null)
        {
            return;
        }

        RefreshSelectedClientRuntimeState(allowAutoRebind: true);
        StatusMessage = $"{SelectedClient.DisplayName}: {SelectedClientBindingDetail}";
        _ = RefreshPreviewAsync();
    }

    private async Task RefreshPreviewAsync()
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (SelectedClient is null)
            {
                PreviewImage = null;
                PreviewWidth = 960;
                PreviewHeight = 540;
                PreviewStatus = "Select a client.";
                PolygonChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var snapshot = _previewService.Capture(SelectedClient);
            PreviewImage = snapshot.Image;
            PreviewWidth = snapshot.ClientWidth;
            PreviewHeight = snapshot.ClientHeight;
            PreviewStatus = snapshot.Status;
            RefreshSelectedClientRuntimeState(allowAutoRebind: true);
            PolygonChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void StartNewPolygon()
    {
        if (SelectedClient is null)
        {
            return;
        }

        SelectedClient.ActionPolygon.Vertices.Clear();
        SelectedClient.ActionPolygon.IsClosed = false;
        SelectedVertexIndex = null;
        StatusMessage = $"{SelectedClient.DisplayName}: polygon reset. Click inside the preview to add vertices.";
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClosePolygon()
    {
        if (SelectedClient is null || SelectedClient.ActionPolygon.Vertices.Count < 3)
        {
            return;
        }

        SelectedClient.ActionPolygon.IsClosed = true;
        StatusMessage = $"{SelectedClient.DisplayName}: polygon closed.";
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteSelectedVertex()
    {
        if (SelectedClient is null || SelectedVertexIndex is null)
        {
            return;
        }

        SelectedClient.ActionPolygon.Vertices.RemoveAt(SelectedVertexIndex.Value);
        if (SelectedClient.ActionPolygon.Vertices.Count < 3)
        {
            SelectedClient.ActionPolygon.IsClosed = false;
        }

        SelectedVertexIndex = null;
        StatusMessage = $"{SelectedClient.DisplayName}: vertex removed.";
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ClearPolygon()
    {
        if (SelectedClient is null)
        {
            return;
        }

        SelectedClient.ActionPolygon.Vertices.Clear();
        SelectedClient.ActionPolygon.IsClosed = false;
        SelectedVertexIndex = null;
        StatusMessage = $"{SelectedClient.DisplayName}: polygon cleared.";
        PolygonChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddTrace()
    {
        if (SelectedClient is null)
        {
            return;
        }

        var trace = new TraceSequence
        {
            Name = string.IsNullOrWhiteSpace(NewTraceName) ? $"Trace {SelectedClient.TraceSequences.Count + 1}" : NewTraceName.Trim(),
        };
        SelectedClient.TraceSequences.Add(trace);
        SelectedTrace = trace;
        NewTraceName = string.Empty;
        StatusMessage = $"{SelectedClient.DisplayName}: trace '{trace.Name}' added.";
    }

    private void RemoveSelectedTrace()
    {
        if (SelectedClient is null || SelectedTrace is null)
        {
            return;
        }

        string name = SelectedTrace.Name;
        string? removedId = SelectedTrace.Id;
        SelectedClient.TraceSequences.Remove(SelectedTrace);
        foreach (var binding in SelectedClient.Bindings.Where(x => x.TraceSequenceId == removedId))
        {
            binding.TraceSequenceId = null;
        }

        SelectedTrace = SelectedClient.TraceSequences.FirstOrDefault();
        StatusMessage = $"{SelectedClient.DisplayName}: trace '{name}' removed.";
    }

    private void RemoveSelectedTracePoint()
    {
        if (SelectedTrace is null || SelectedTracePoint is null)
        {
            return;
        }

        int index = SelectedTrace.Points.IndexOf(SelectedTracePoint);
        if (index < 0)
        {
            return;
        }

        SelectedTrace.Points.RemoveAt(index);
        SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
        SelectedTracePoint = SelectedTrace.Points.ElementAtOrDefault(Math.Clamp(index, 0, SelectedTrace.Points.Count - 1));
        StatusMessage = $"{SelectedTrace.Name}: point removed.";
    }

    private void ClearTracePoints()
    {
        if (SelectedTrace is null || SelectedTrace.Points.Count == 0)
        {
            return;
        }

        SelectedTrace.Points.Clear();
        SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
        SelectedTracePoint = null;
        StatusMessage = $"{SelectedTrace.Name}: all points cleared.";
    }

    private bool CanMoveSelectedTracePoint(int offset)
    {
        if (SelectedTrace is null || SelectedTracePoint is null)
        {
            return false;
        }

        int index = SelectedTrace.Points.IndexOf(SelectedTracePoint);
        if (index < 0)
        {
            return false;
        }

        int targetIndex = index + offset;
        return targetIndex >= 0 && targetIndex < SelectedTrace.Points.Count;
    }

    private void MoveSelectedTracePoint(int offset)
    {
        if (!CanMoveSelectedTracePoint(offset) || SelectedTrace is null || SelectedTracePoint is null)
        {
            return;
        }

        int index = SelectedTrace.Points.IndexOf(SelectedTracePoint);
        int targetIndex = index + offset;
        SelectedTrace.Points.Move(index, targetIndex);
        SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
        SelectedTracePoint = SelectedTrace.Points[targetIndex];
        StatusMessage = $"{SelectedTrace.Name}: point order updated.";
    }

    private void NudgeSelectedTracePoint(double deltaX, double deltaY)
    {
        if (SelectedTracePoint is null || SelectedTrace is null)
        {
            return;
        }

        SelectedTracePoint.X = Math.Clamp(SelectedTracePoint.X + deltaX, 0d, 1d);
        SelectedTracePoint.Y = Math.Clamp(SelectedTracePoint.Y + deltaY, 0d, 1d);
        SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
        RaisePropertyChanged(nameof(SelectedTracePointSummary));
        StatusMessage = $"{SelectedTrace.Name}: selected point nudged.";
    }

    private void StartTraceRecording()
    {
        if (SelectedClient is null || SelectedTrace is null)
        {
            return;
        }

        // Arm the hook, then let TraceRecorder bring game to foreground.
        // We pass a callback that minimizes this tool window so the user's view goes straight to the game.
        var status = _traceRecorder.Start(SelectedClient, onStartFocus: () =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (System.Windows.Application.Current.MainWindow is Window w)
                    w.WindowState = System.Windows.WindowState.Minimized;
            });
        });
        IsTraceRecording = _traceRecorder.IsRecording;
        StatusMessage = status;
    }

    private void StopTraceRecording()
    {
        _traceRecorder.Stop();
        IsTraceRecording = false;
        StatusMessage = "Trace recording stopped.";

        // Restore tool window and refresh preview so dots appear on the captured screenshot.
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (System.Windows.Application.Current.MainWindow is Window w)
                w.WindowState = System.Windows.WindowState.Normal;
        });
        _ = RefreshPreviewAsync();
    }

    private void AddBinding()
    {
        if (SelectedClient is null)
        {
            return;
        }

        var binding = new MacroBinding
        {
            ClientProfileId = SelectedClient.Id,
            Name = $"{SelectedClient.DisplayName} Binding {SelectedClient.Bindings.Count + 1}",
            TraceSequenceId = SelectedClient.TraceSequences.FirstOrDefault()?.Id,
        };

        SelectedClient.Bindings.Add(binding);
        SelectedBinding = binding;
        StatusMessage = $"{SelectedClient.DisplayName}: binding '{binding.Name}' added.";
    }

    private void RemoveSelectedBinding()
    {
        if (SelectedClient is null || SelectedBinding is null)
        {
            return;
        }

        string name = SelectedBinding.Name;
        SelectedClient.Bindings.Remove(SelectedBinding);
        SelectedBinding = SelectedClient.Bindings.FirstOrDefault();
        StatusMessage = $"{SelectedClient.DisplayName}: binding '{name}' removed.";
    }

    private void AttachConfigSubscriptions()
    {
        ClientProfiles.CollectionChanged += OnProfilesCollectionChanged;
        foreach (var profile in ClientProfiles)
        {
            AttachProfileSubscriptions(profile);
        }
    }

    private void OnProfilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (ClientProfile profile in e.NewItems)
            {
                AttachProfileSubscriptions(profile);
            }
        }

        if (e.OldItems is not null)
        {
            foreach (ClientProfile profile in e.OldItems)
            {
                DetachProfileSubscriptions(profile);
            }
        }

        BindingValidator.NormalizeConfig(_config);
        RefreshHotkeys();
        RefreshCommandStates();
    }

    private void AttachProfileSubscriptions(ClientProfile profile)
    {
        profile.PropertyChanged += OnProfilePropertyChanged;
        profile.Bindings.CollectionChanged += OnBindingsCollectionChanged;
        profile.TraceSequences.CollectionChanged += OnTraceSequencesCollectionChanged;
        foreach (var binding in profile.Bindings)
        {
            binding.PropertyChanged += OnBindingPropertyChanged;
        }
    }

    private void DetachProfileSubscriptions(ClientProfile profile)
    {
        profile.PropertyChanged -= OnProfilePropertyChanged;
        profile.Bindings.CollectionChanged -= OnBindingsCollectionChanged;
        profile.TraceSequences.CollectionChanged -= OnTraceSequencesCollectionChanged;
        foreach (var binding in profile.Bindings)
        {
            binding.PropertyChanged -= OnBindingPropertyChanged;
        }
    }

    private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ClientProfile.IsEnabled) or nameof(ClientProfile.BoundWindow))
        {
            RefreshHotkeys();
        }
    }

    private void OnBindingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
        {
            foreach (MacroBinding binding in e.NewItems)
            {
                binding.PropertyChanged += OnBindingPropertyChanged;
            }
        }

        if (e.OldItems is not null)
        {
            foreach (MacroBinding binding in e.OldItems)
            {
                binding.PropertyChanged -= OnBindingPropertyChanged;
            }
        }

        BindingValidator.NormalizeConfig(_config);
        RefreshHotkeys();
        RefreshCommandStates();
    }

    private void OnTraceSequencesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshCommandStates();
    }

    private void OnBindingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MacroBinding.TriggerHotkey) or nameof(MacroBinding.IsEnabled))
        {
            RefreshHotkeys();
        }
    }

    private void RefreshHotkeys()
    {
        BindingValidator.NormalizeConfig(_config);

        if (!_hotkeyService.IsReady)
        {
            HotkeyStatus = "Hotkeys will register when the main window is ready.";
            return;
        }

        var duplicates = BindingValidator.GetDuplicateHotkeys(_config);
        if (duplicates.Count > 0)
        {
            _hotkeyService.UnregisterAll();
            HotkeyStatus = $"Duplicate hotkeys: {string.Join(", ", duplicates)}";
            return;
        }

        var errors = _hotkeyService.RegisterBindings(ClientProfiles.SelectMany(profile => profile.Bindings));
        HotkeyStatus = errors.Count == 0
            ? "Global hotkeys registered."
            : string.Join(" | ", errors);
    }

    private async void OnHotkeyPressed(object? sender, string hotkey)
    {
        string status = await _macroExecutor.ExecuteHotkeyAsync(_config, hotkey);
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => StatusMessage = status);
    }

    private async void OnTracePointCaptured(object? sender, NormalizedPoint point)
    {
        if (SelectedTrace is null)
        {
            return;
        }

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            SelectedTrace.Points.Add(point);
            SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
            SelectedTracePoint = point;
            StatusMessage = $"{SelectedTrace.Name}: {SelectedTrace.Points.Count} point(s) recorded.";
            RefreshCommandStates();
            TracePointsChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnSelectedTracePointPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NormalizedPoint.X) or nameof(NormalizedPoint.Y))
        {
            if (SelectedTrace is not null)
            {
                SelectedTrace.LastUpdatedUtc = DateTimeOffset.UtcNow;
            }

            RaisePropertyChanged(nameof(SelectedTracePointSummary));
        }
    }

    private ClientWindowRef? FindMatchingAvailableWindow(ClientWindowRef? boundWindow)
    {
        if (boundWindow is null)
        {
            return null;
        }

        return AvailableWindows.FirstOrDefault(window => window.WindowHandle == boundWindow.WindowHandle)
            ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId
                && string.Equals(window.WindowTitle, boundWindow.WindowTitle, StringComparison.OrdinalIgnoreCase))
            ?? AvailableWindows.FirstOrDefault(window => window.ProcessId == boundWindow.ProcessId);
    }

    private void RefreshCommandStates()
    {
        RemoveSelectedClientCommand.RaiseCanExecuteChanged();
        BindSelectedClientCommand.RaiseCanExecuteChanged();
        UnbindSelectedClientCommand.RaiseCanExecuteChanged();
        ResolveSelectedClientCommand.RaiseCanExecuteChanged();
        RefreshPreviewCommand.RaiseCanExecuteChanged();
        NewPolygonCommand.RaiseCanExecuteChanged();
        ClosePolygonCommand.RaiseCanExecuteChanged();
        DeleteSelectedVertexCommand.RaiseCanExecuteChanged();
        ClearPolygonCommand.RaiseCanExecuteChanged();
        AddTraceCommand.RaiseCanExecuteChanged();
        RemoveTraceCommand.RaiseCanExecuteChanged();
        RemoveTracePointCommand.RaiseCanExecuteChanged();
        MoveTracePointUpCommand.RaiseCanExecuteChanged();
        MoveTracePointDownCommand.RaiseCanExecuteChanged();
        NudgeTracePointUpCommand.RaiseCanExecuteChanged();
        NudgeTracePointDownCommand.RaiseCanExecuteChanged();
        NudgeTracePointLeftCommand.RaiseCanExecuteChanged();
        NudgeTracePointRightCommand.RaiseCanExecuteChanged();
        ClearTracePointsCommand.RaiseCanExecuteChanged();
        StartTraceCommand.RaiseCanExecuteChanged();
        StopTraceCommand.RaiseCanExecuteChanged();
        AddBindingCommand.RaiseCanExecuteChanged();
        RemoveBindingCommand.RaiseCanExecuteChanged();
    }

    public void Dispose()
    {
        StopTraceRecording();
        SaveConfig();
        _traceRecorder.PointCaptured -= OnTracePointCaptured;
        _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
        _hotkeyService.UnregisterAll();
        _traceRecorder.Dispose();
    }
}
