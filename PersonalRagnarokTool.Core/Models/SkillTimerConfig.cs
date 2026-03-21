using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class SkillTimerEntry : ObservableObject
{
    private string _key = "None";
    private int _delaySeconds = 10;
    private bool _enabled;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public int DelaySeconds
    {
        get => _delaySeconds;
        set => SetProperty(ref _delaySeconds, Math.Max(1, value));
    }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }
}

public sealed class SkillTimerConfig : ObservableObject
{
    private bool _enabled;

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public SkillTimerEntry Timer1 { get; set; } = new();
    public SkillTimerEntry Timer2 { get; set; } = new();
    public SkillTimerEntry Timer3 { get; set; } = new();
}
