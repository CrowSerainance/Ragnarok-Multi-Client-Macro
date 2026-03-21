using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class BuffConfig : ObservableObject
{
    private uint _statusId;
    private string _key = "None";
    private bool _enabled = true;
    private string _name = "Unknown Buff";

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

public sealed class AutobuffConfig : ObservableObject
{
    private bool _enabled;
    public ObservableCollection<BuffConfig> Buffs { get; set; } = new();

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

public sealed class AutobuffSkillsConfig : ObservableObject
{
    private bool _enabled;
    public ObservableCollection<BuffConfig> Buffs { get; set; } = new();

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

public sealed class AutobuffItemsConfig : ObservableObject
{
    private bool _enabled;
    public ObservableCollection<BuffConfig> Buffs { get; set; } = new();

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}
