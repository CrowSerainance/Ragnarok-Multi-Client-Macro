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
    private ExecutionMode _executionMode = ExecutionMode.TraceSequence;
    private string? _traceSequenceId;
    private int _postInputDelayMs = 120;
    private int _interClickDelayMs = 80;
    private int? _clickCountOverride;

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

    public ExecutionMode ExecutionMode
    {
        get => _executionMode;
        set => SetProperty(ref _executionMode, value);
    }

    public string? TraceSequenceId
    {
        get => _traceSequenceId;
        set => SetProperty(ref _traceSequenceId, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
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

    public int? ClickCountOverride
    {
        get => _clickCountOverride;
        set => SetProperty(ref _clickCountOverride, value is null ? null : Math.Max(1, value.Value));
    }
}
