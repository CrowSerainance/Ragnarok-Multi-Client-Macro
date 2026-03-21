using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class YggAutopotConfig : ObservableObject
{
    private bool _enabled;
    private string _hpKey = "None";
    private int _hpThreshold = 20;
    private string _spKey = "None";
    private int _spThreshold = 20;
    private int _delayMs = 50;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public string HpKey
    {
        get => _hpKey;
        set => SetProperty(ref _hpKey, value);
    }

    public int HpThreshold
    {
        get => _hpThreshold;
        set => SetProperty(ref _hpThreshold, value);
    }

    public string SpKey
    {
        get => _spKey;
        set => SetProperty(ref _spKey, value);
    }

    public int SpThreshold
    {
        get => _spThreshold;
        set => SetProperty(ref _spThreshold, value);
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, value);
    }
}
