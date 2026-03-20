using System.Collections.ObjectModel;
using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class SpammerKey : ObservableObject
{
    private string _key = "None";
    private int _intervalMs = 100;
    private bool _enabled = true;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public int IntervalMs
    {
        get => _intervalMs;
        set => SetProperty(ref _intervalMs, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

public sealed class SpammerConfig : ObservableObject
{
    private bool _enabled;
    public ObservableCollection<SpammerKey> Keys { get; set; } = new();

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}
