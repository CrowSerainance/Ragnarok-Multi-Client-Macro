using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

/// <summary>
/// Aggressive input dispatcher using hardware-level SendInput with a focus-flash technique.
/// Briefly steals focus to the game window (~50ms), fires SendInput, then restores the
/// original foreground window. This bypasses PostMessage filtering by Gepard/GameGuard.
/// Memory writes for mouse position are also performed when the attachment is live.
/// </summary>
public sealed class BackgroundInputDispatcher
{
    private readonly MemoryService _memoryService;
    private readonly ProcessAttachmentService _attachmentService;
    private readonly Random _rng = new();
    private readonly object _focusLock = new();

    public BackgroundInputDispatcher(MemoryService memoryService, ProcessAttachmentService attachmentService)
    {
        _memoryService = memoryService;
        _attachmentService = attachmentService;
    }

    public bool SendInputKey(ClientWindowRef window, string? inputKey, out string status)
    {
        status = string.Empty;
        if (string.IsNullOrWhiteSpace(inputKey))
        {
            return true;
        }

        if (!VirtualKeyMap.TryGetVirtualKey(inputKey, out int virtualKey))
        {
            status = $"Unsupported input key '{inputKey}'.";
            return false;
        }

        IntPtr gameHwnd = ResolveGameWindowHandle(window);
        if (gameHwnd == IntPtr.Zero)
        {
            status = "No valid game window handle found.";
            return false;
        }

        ushort scanCode = (ushort)NativeMethods.MapVirtualKeyW((uint)virtualKey, 0);

        // Hardware-level key press via SendInput inside a focus-flash.
        FlashFocusAndExecute(gameHwnd, () =>
        {
            var inputs = new NativeMethods.INPUT[2];

            // Key down
            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)virtualKey;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYDOWN;

            // Key up
            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = (ushort)virtualKey;
            inputs[1].u.ki.wScan = scanCode;
            inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(1, new[] { inputs[0] }, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(_rng.Next(25, 55));
            NativeMethods.SendInput(1, new[] { inputs[1] }, Marshal.SizeOf<NativeMethods.INPUT>());
        });

        status = $"{HotkeyText.Normalize(inputKey)} sent via SendInput on 0x{gameHwnd.ToInt64():X}.";
        return true;
    }

    public void SendClick(ClientWindowRef window, int clientX, int clientY, AppConfig config)
    {
        IntPtr gameHwnd = ResolveGameWindowHandle(window);
        if (gameHwnd == IntPtr.Zero || !NativeMethods.IsWindow(gameHwnd))
            return;

        // Write mouse position to game memory if the tight connection is live.
        bool attachmentMatchesTarget = _attachmentService.IsAttached
            && _attachmentService.ProcessId == window.ProcessId;

        if (attachmentMatchesTarget && _memoryService.IsValid && _memoryService.BaseAddress != IntPtr.Zero)
        {
            _memoryService.WriteUInt32(IntPtr.Add(_memoryService.BaseAddress, config.MousePosXAddress), (uint)clientX);
            _memoryService.WriteUInt32(IntPtr.Add(_memoryService.BaseAddress, config.MousePosYAddress), (uint)clientY);
        }

        // Convert client coords to screen coords for SendInput.
        var clientOrigin = new NativeMethods.POINT();
        NativeMethods.ClientToScreen(gameHwnd, ref clientOrigin);
        int screenX = clientOrigin.X + clientX;
        int screenY = clientOrigin.Y + clientY;

        // Save current mouse position so we can restore it after the click.
        NativeMethods.GetCursorPos(out var savedCursor);

        FlashFocusAndExecute(gameHwnd, () =>
        {
            // Move the real cursor to the target position.
            NativeMethods.SetCursorPos(screenX, screenY);
            Thread.Sleep(_rng.Next(5, 12));

            // Mouse down
            var downInput = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = NativeMethods.MOUSEEVENTF_LEFTDOWN,
                    }
                }
            };
            NativeMethods.SendInput(1, new[] { downInput }, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(_rng.Next(25, 55));

            // Mouse up
            var upInput = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT
                    {
                        dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP,
                    }
                }
            };
            NativeMethods.SendInput(1, new[] { upInput }, Marshal.SizeOf<NativeMethods.INPUT>());
        });

        // Restore cursor position
        NativeMethods.SetCursorPos(savedCursor.X, savedCursor.Y);

        if (attachmentMatchesTarget && _memoryService.IsValid)
        {
            _attachmentService.CloseWriteHandle();
        }
    }

    /// <summary>
    /// Briefly brings the game window to the foreground, executes the action (SendInput calls),
    /// then restores the previous foreground window. The entire flash takes ~50-100ms.
    /// Uses AttachThreadInput to guarantee SetForegroundWindow succeeds.
    /// </summary>
    private void FlashFocusAndExecute(IntPtr gameHwnd, Action action)
    {
        lock (_focusLock)
        {
            IntPtr previousFg = NativeMethods.GetForegroundWindow();
            uint myThreadId = NativeMethods.GetCurrentThreadId();
            uint fgThreadId = NativeMethods.GetWindowThreadProcessId(previousFg, out _);
            bool attached = false;

            try
            {
                // Attach our thread input to the foreground thread so SetForegroundWindow is allowed.
                if (myThreadId != fgThreadId)
                {
                    attached = NativeMethods.AttachThreadInput(myThreadId, fgThreadId, true);
                }

                NativeMethods.SetForegroundWindow(gameHwnd);
                Thread.Sleep(10); // tiny wait for focus to settle

                action();

                Thread.Sleep(5);
            }
            finally
            {
                // Restore previous foreground window.
                if (previousFg != IntPtr.Zero && previousFg != gameHwnd)
                {
                    NativeMethods.SetForegroundWindow(previousFg);
                }

                if (attached)
                {
                    NativeMethods.AttachThreadInput(myThreadId, fgThreadId, false);
                }
            }
        }
    }

    private IntPtr ResolveGameWindowHandle(ClientWindowRef window)
    {
        if (_attachmentService.IsAttached && _attachmentService.ProcessId == window.ProcessId)
        {
            IntPtr attachedHandle = _attachmentService.WindowHandle;
            if (attachedHandle != IntPtr.Zero && NativeMethods.IsWindow(attachedHandle))
            {
                return attachedHandle;
            }
        }

        IntPtr directHandle = new(window.WindowHandle);
        if (directHandle != IntPtr.Zero && NativeMethods.IsWindow(directHandle))
        {
            return directHandle;
        }

        IntPtr resolvedHandle = IntPtr.Zero;
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
            if (processId == (uint)window.ProcessId)
            {
                resolvedHandle = hWnd;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return resolvedHandle;
    }
}
