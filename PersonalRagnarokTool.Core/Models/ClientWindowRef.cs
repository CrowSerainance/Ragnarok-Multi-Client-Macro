using PersonalRagnarokTool.Core.Infrastructure;

namespace PersonalRagnarokTool.Core.Models;

public sealed class ClientWindowRef : ObservableObject
{
    private long _windowHandle;
    private int _processId;
    private string _processName = string.Empty;
    private string _windowTitle = string.Empty;
    private int _clientWidth;
    private int _clientHeight;

    public long WindowHandle
    {
        get => _windowHandle;
        set => SetProperty(ref _windowHandle, value);
    }

    public int ProcessId
    {
        get => _processId;
        set => SetProperty(ref _processId, value);
    }

    public string ProcessName
    {
        get => _processName;
        set => SetProperty(ref _processName, value ?? string.Empty);
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value ?? string.Empty);
    }

    public int ClientWidth
    {
        get => _clientWidth;
        set => SetProperty(ref _clientWidth, Math.Max(0, value));
    }

    public int ClientHeight
    {
        get => _clientHeight;
        set => SetProperty(ref _clientHeight, Math.Max(0, value));
    }

    public string DisplayText =>
        ProcessId > 0
            ? $"{WindowTitle} ({ProcessName}:{ProcessId})"
            : "Unbound";
}
