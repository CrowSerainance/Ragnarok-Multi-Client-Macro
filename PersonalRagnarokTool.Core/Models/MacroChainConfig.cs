using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class MacroChainEntry : ObservableObject
{
    private string _key = "None";
    private int _delayMs = 50;
    private bool _hasClick;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, Math.Max(0, value));
    }

    public bool HasClick
    {
        get => _hasClick;
        set => SetProperty(ref _hasClick, value);
    }
}

public sealed class MacroChainLane : ObservableObject
{
    private int _id;
    private string _triggerKey = "None";
    private string _daggerKey = "None";
    private string _instrumentKey = "None";
    private int _delayMs = 50;
    private bool _infinityLoop;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string TriggerKey
    {
        get => _triggerKey;
        set => SetProperty(ref _triggerKey, value);
    }

    public string DaggerKey
    {
        get => _daggerKey;
        set => SetProperty(ref _daggerKey, value);
    }

    public string InstrumentKey
    {
        get => _instrumentKey;
        set => SetProperty(ref _instrumentKey, value);
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, Math.Max(0, value));
    }

    public bool InfinityLoop
    {
        get => _infinityLoop;
        set => SetProperty(ref _infinityLoop, value);
    }

    public ObservableCollection<MacroChainEntry> Entries { get; set; } = new();
}

public sealed class MacroSongConfig : ObservableObject
{
    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public ObservableCollection<MacroChainLane> Lanes { get; set; } = new()
    {
        new MacroChainLane { Id = 1 },
        new MacroChainLane { Id = 2 },
        new MacroChainLane { Id = 3 },
        new MacroChainLane { Id = 4 },
    };
}

public sealed class MacroSwitchConfig : ObservableObject
{
    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public ObservableCollection<MacroChainLane> Lanes { get; set; } = new()
    {
        new MacroChainLane { Id = 1 },
        new MacroChainLane { Id = 2 },
        new MacroChainLane { Id = 3 },
        new MacroChainLane { Id = 4 },
        new MacroChainLane { Id = 5 },
    };
}
