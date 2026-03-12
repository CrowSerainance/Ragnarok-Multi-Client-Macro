using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class AppConfig : ObservableObject
{
    private int _version = 1;
    private DateTimeOffset _lastSavedUtc = DateTimeOffset.UtcNow;

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

    private int _mousePosXAddress = 0xB47F60;
    private int _mousePosYAddress = 0xB47F64;

    public int MousePosXAddress
    {
        get => _mousePosXAddress;
        set => SetProperty(ref _mousePosXAddress, value);
    }

    public int MousePosYAddress
    {
        get => _mousePosYAddress;
        set => SetProperty(ref _mousePosYAddress, value);
    }

    public ObservableCollection<ClientProfile> ClientProfiles { get; set; } = new();
}
