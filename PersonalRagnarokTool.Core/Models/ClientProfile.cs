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
    private string _runtimeStatusLabel = "Unbound";
    private string _runtimeStatusDetail = "Bind this profile to a live client window.";
    private bool _hasLiveWindow;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, string.IsNullOrWhiteSpace(value) ? "Client" : value.Trim());
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

    public ObservableCollection<MacroBinding> Bindings { get; set; } = new();

    public string BoundWindowDisplayText => BoundWindow?.DisplayText ?? "Not bound";

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
        set => SetProperty(ref _runtimeStatusDetail, value ?? "Bind this profile to a live client window.");
    }

    [JsonIgnore]
    public bool HasLiveWindow
    {
        get => _hasLiveWindow;
        set => SetProperty(ref _hasLiveWindow, value);
    }
}
