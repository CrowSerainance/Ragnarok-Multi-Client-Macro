using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class ClientProfile : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _displayName = "Client";
    private bool _isEnabled = true;
    private ClientWindowRef? _boundWindow;
    private ToggleCombo? _clientToggle;
    private bool _isActive;
    private string _runtimeStatusLabel = "Unbound";
    private string _runtimeStatusDetail = "Bind this profile to a live client window.";
    private bool _hasLiveWindow;

    public string Id
    {
        get => _id;
        set
        {
            var v = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(v)) return;
            SetProperty(ref _id, v);
        }
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed)) trimmed = "Client";
            SetProperty(ref _displayName, trimmed);
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public ClientWindowRef? BoundWindow
    {
        get => _boundWindow;
        set
        {
            if (SetProperty(ref _boundWindow, value))
            {
                RaisePropertyChanged(nameof(BoundWindowDisplayText));
            }
        }
    }

    public ToggleCombo? ClientToggle
    {
        get => _clientToggle;
        set => SetProperty(ref _clientToggle, value);
    }

    public ObservableCollection<MacroBinding> Bindings { get; init; } = new();

    public AutopotConfig Autopot { get; set; } = new();
    public AutobuffConfig Autobuff { get; set; } = new();
    public SpammerConfig Spammer { get; set; } = new();
    public StatusRecoveryConfig Recovery { get; set; } = new();

    public string BoundWindowDisplayText => BoundWindow?.DisplayText ?? "Not bound";

    [JsonIgnore]
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    [JsonIgnore]
    public string RuntimeStatusLabel
    {
        get => _runtimeStatusLabel;
        set => SetProperty(ref _runtimeStatusLabel, value ?? "Unbound");
    }

    [JsonIgnore]
    public string RuntimeStatusDetail
    {
        get => _runtimeStatusDetail;
        set => SetProperty(ref _runtimeStatusDetail, value ?? string.Empty);
    }

    [JsonIgnore]
    public bool HasLiveWindow
    {
        get => _hasLiveWindow;
        set => SetProperty(ref _hasLiveWindow, value);
    }
}
