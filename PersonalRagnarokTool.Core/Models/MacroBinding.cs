using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class MacroBinding : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _clientProfileId = string.Empty;
    private string _name = "Binding";
    private bool _isEnabled = true;
    private string _triggerHotkey = "F1";
    private string _inputKey = "F1";
    private int _cellRadius = 5;
    private int _postInputDelayMs = 120;
    private int _interClickDelayMs = 80;
    private int _clickCount = 1;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
    }

    public string ClientProfileId
    {
        get => _clientProfileId;
        set => SetProperty(ref _clientProfileId, value?.Trim() ?? string.Empty);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, string.IsNullOrWhiteSpace(value) ? "Binding" : value.Trim());
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string TriggerHotkey
    {
        get => _triggerHotkey;
        set => SetProperty(ref _triggerHotkey, value?.Trim() ?? string.Empty);
    }

    public string InputKey
    {
        get => _inputKey;
        set => SetProperty(ref _inputKey, value?.Trim() ?? string.Empty);
    }

    public int CellRadius
    {
        get => _cellRadius;
        set => SetProperty(ref _cellRadius, Core.Geometry.CellMath.ClampRadius(value));
    }

    public int PostInputDelayMs
    {
        get => _postInputDelayMs;
        set => SetProperty(ref _postInputDelayMs, Math.Max(0, value));
    }

    public int InterClickDelayMs
    {
        get => _interClickDelayMs;
        set => SetProperty(ref _interClickDelayMs, Math.Max(0, value));
    }

    public int ClickCount
    {
        get => _clickCount;
        set => SetProperty(ref _clickCount, Math.Max(1, value));
    }
}
