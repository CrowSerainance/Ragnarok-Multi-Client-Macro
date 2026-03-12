using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Geometry;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Services;

public sealed class TraceRecorder : IDisposable
{
    private readonly ClientBindingService _bindingService;
    private readonly NativeMethods.HookProc _mouseHookCallback;
    private IntPtr _hookHandle = IntPtr.Zero;
    private ClientProfile? _activeProfile;
    private Action? _onStartFocus;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    public TraceRecorder(ClientBindingService bindingService)
    {
        _bindingService = bindingService;
        _mouseHookCallback = MouseHookCallback;
    }

    public event EventHandler<NormalizedPoint>? PointCaptured;

    public bool IsRecording => _hookHandle != IntPtr.Zero && _activeProfile is not null;

    /// <summary>
    /// Arms the low-level mouse hook and optionally invokes <paramref name="onStartFocus"/>
    /// (e.g. minimize the tool window) so the user is immediately looking at the game.
    /// The game window is brought to the foreground automatically.
    /// </summary>
    public string Start(ClientProfile profile, Action? onStartFocus = null)
    {
        Stop();
        _activeProfile = profile;
        _onStartFocus = onStartFocus;

        IntPtr moduleHandle = NativeMethods.GetModuleHandle(null);
        _hookHandle = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, _mouseHookCallback, moduleHandle, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            _activeProfile = null;
            return "Failed to install mouse trace hook.";
        }

        // Bring game window to front so user immediately starts clicking there.
        var liveWindow = _bindingService.ResolveLiveWindow(profile);
        if (liveWindow?.WindowHandle != 0)
        {
            SetForegroundWindow(new IntPtr(liveWindow!.WindowHandle));
        }

        // Run the caller's extra focus action (e.g. minimize the tool window).
        _onStartFocus?.Invoke();

        return $"Recording clicks for {profile.DisplayName}. Click on the game window, then press Stop.";
    }

    public void Stop()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            _ = NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _activeProfile = null;
        _onStartFocus = null;
    }

    private IntPtr MouseHookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0 && _activeProfile is not null && wParam.ToInt32() == NativeMethods.WM_LBUTTONDOWN)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            TryCapturePoint(_activeProfile, hookStruct.pt.X, hookStruct.pt.Y);
        }

        return NativeMethods.CallNextHookEx(_hookHandle, code, wParam, lParam);
    }

    private void TryCapturePoint(ClientProfile profile, int screenX, int screenY)
    {
        var liveWindow = _bindingService.ResolveLiveWindow(profile);
        if (liveWindow is null)
        {
            return;
        }

        IntPtr handle = new(liveWindow.WindowHandle);
        var clientOrigin = new NativeMethods.POINT();
        if (!NativeMethods.ClientToScreen(handle, ref clientOrigin))
        {
            return;
        }

        int clientWidth = liveWindow.ClientWidth;
        int clientHeight = liveWindow.ClientHeight;
        if (screenX < clientOrigin.X
            || screenY < clientOrigin.Y
            || screenX > clientOrigin.X + clientWidth
            || screenY > clientOrigin.Y + clientHeight)
        {
            return;
        }

        int clientX = screenX - clientOrigin.X;
        int clientY = screenY - clientOrigin.Y;
        PointCaptured?.Invoke(this, CoordinateTranslator.ToNormalized(clientX, clientY, clientWidth, clientHeight));
    }

    public void Dispose() => Stop();
}
