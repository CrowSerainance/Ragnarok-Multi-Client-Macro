using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class AppConfig : ObservableObject
{
    private int _version = 1;
    private DateTimeOffset _lastSavedUtc = DateTimeOffset.UtcNow;
    private InputMethod _inputMethod = InputMethod.PostMessage;
    private ToggleCombo _globalToggle = new();

    public int Version
    {
        get => _version;
        set => SetProperty(ref _version, value);
    }

    public DateTimeOffset LastSavedUtc
    {
        get => _lastSavedUtc;
        set => SetProperty(ref _lastSavedUtc, value);
    }

    public InputMethod InputMethod
    {
        get => _inputMethod;
        set => SetProperty(ref _inputMethod, value);
    }

    public ToggleCombo GlobalToggle
    {
        get => _globalToggle;
        set => SetProperty(ref _globalToggle, value ?? new());
    }

    public ObservableCollection<ClientProfile> ClientProfiles { get; set; } = new();
    public ServerListConfig Servers { get; set; } = new();
}
