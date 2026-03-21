using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class RecoveryConfig : ObservableObject
{
    private uint _statusId;
    private string _key = "None";
    private bool _enabled = true;
    private string _name = "Unknown Status";

    public uint StatusId
    {
        get => _statusId;
        set => SetProperty(ref _statusId, value);
    }

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}

public sealed class StatusRecoveryConfig : ObservableObject
{
    private bool _enabled;
    public ObservableCollection<RecoveryConfig> Recoveries { get; set; } = new();

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

public sealed class DebuffRecoveryConfig : ObservableObject
{
    private bool _enabled;
    private string _groupStatusKey = "None";
    private string _groupNewStatusKey = "None";
    private bool _autoStand;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public string GroupStatusKey
    {
        get => _groupStatusKey;
        set => SetProperty(ref _groupStatusKey, value);
    }

    public string GroupNewStatusKey
    {
        get => _groupNewStatusKey;
        set => SetProperty(ref _groupNewStatusKey, value);
    }

    public bool AutoStand
    {
        get => _autoStand;
        set => SetProperty(ref _autoStand, value);
    }

    public ObservableCollection<RecoveryConfig> Recoveries { get; set; } = new();
}
