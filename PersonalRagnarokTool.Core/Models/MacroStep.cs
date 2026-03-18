using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class MacroStep : ObservableObject
{
    private string _key = "F1";
    private int _delayMs = 100;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, string.IsNullOrWhiteSpace(value) ? "F1" : value.Trim());
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, Math.Max(0, value));
    }
}
