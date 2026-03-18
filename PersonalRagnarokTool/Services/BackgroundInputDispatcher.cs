using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;
using PersonalRagnarokTool.Core.Services;

namespace PersonalRagnarokTool.Services;

/// <summary>
/// Dispatches input to game windows.
/// 
/// PostMessage mode: sends WM_KEYDOWN/WM_KEYUP and WM_LBUTTONDOWN/WM_LBUTTONUP
/// directly to the target window handle. No focus change. Works on background windows.
/// Does not depend on ProcessAttachmentService for key/click delivery.
/// 
/// SendInput mode (fallback): focus-flash technique for Gepard/GameGuard-protected
/// servers that filter PostMessage. Requires brief foreground steal.
/// 
/// Memory-backed mouse coordinate writes are opportunistic: performed when the
/// global ProcessAttachmentService happens to be attached to the same process as
/// the target window. Skipped silently otherwise — PostMessage click coordinates
/// in lParam are sufficient for most RO clients.
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

    // ═══════════════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════════════

    public bool SendInputKey(ClientWindowRef window, string? inputKey, AppConfig config, out string status)
    {
        status = string.Empty;
        if (string.IsNullOrWhiteSpace(inputKey))
            return true;

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

        if (config.InputMethod == InputMethod.PostMessage)
        {
            PostMessageKey(gameHwnd, virtualKey);
            status = $"{HotkeyText.Normalize(inputKey)} sent via PostMessage to 0x{gameHwnd.ToInt64():X}.";
        }
        else
        {
            SendInputKeyViaFocusFlash(gameHwnd, virtualKey);
            status = $"{HotkeyText.Normalize(inputKey)} sent via SendInput to 0x{gameHwnd.ToInt64():X}.";
        }

        return true;
    }

    public void SendClick(ClientWindowRef window, int clientX, int clientY, AppConfig config, InputMethod? methodOverride = null)
    {
        IntPtr gameHwnd = ResolveGameWindowHandle(window);
        if (gameHwnd == IntPtr.Zero || !NativeMethods.IsWindow(gameHwnd))
            return;

        // Opportunistic memory-backed mouse position write.
        // Only performed when the global attachment targets the SAME process.
        bool attachmentMatchesTarget = _attachmentService.IsAttached
            && _attachmentService.ProcessId == window.ProcessId;

        if (attachmentMatchesTarget && _memoryService.IsValid && _memoryService.BaseAddress != IntPtr.Zero)
        {
            _memoryService.WriteUInt32(IntPtr.Add(_memoryService.BaseAddress, config.MousePosXAddress), (uint)clientX);
            _memoryService.WriteUInt32(IntPtr.Add(_memoryService.BaseAddress, config.MousePosYAddress), (uint)clientY);
        }

        var effectiveMethod = methodOverride ?? config.InputMethod;

        if (effectiveMethod == InputMethod.PostMessage)
        {
            PostMessageClick(gameHwnd, clientX, clientY);
        }
        else
        {
            SendInputClickViaFocusFlash(gameHwnd, clientX, clientY);
        }

        if (attachmentMatchesTarget && _memoryService.IsValid)
        {
            _attachmentService.CloseWriteHandle();
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  PostMessage — true background, no focus steal
    // ═══════════════════════════════════════════════════════════

    private void PostMessageKey(IntPtr hwnd, int virtualKey)
    {
        // Build lParam the way the RO client expects:
        // bits 0-15  = repeat count (1)
        // bits 16-23 = scan code
        // bit  24    = extended key flag
        // bits 25-28 = reserved (0)
        // bit  29    = context code (0 for WM_KEYDOWN)
        // bit  30    = previous key state (0 = was up)
        // bit  31    = transition state (0 = pressed)
        uint scanCode = NativeMethods.MapVirtualKeyW((uint)virtualKey, 0);
        IntPtr downLParam = (IntPtr)(1 | (scanCode << 16));
        IntPtr upLParam   = (IntPtr)(1 | (scanCode << 16) | (1 << 30) | (1 << 31));

        NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)virtualKey, downLParam);
        Thread.Sleep(_rng.Next(25, 55));
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYUP, (IntPtr)virtualKey, upLParam);
    }

    private static void PostMessageClick(IntPtr hwnd, int clientX, int clientY)
    {
        IntPtr lParam = MakeLParam(clientX, clientY);

        NativeMethods.PostMessage(hwnd, NativeMethods.WM_LBUTTONDOWN_MESSAGE, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }

    private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    // ═══════════════════════════════════════════════════════════
    //  SendInput + focus-flash (Gepard/GameGuard fallback)
    // ═══════════════════════════════════════════════════════════

    private void SendInputKeyViaFocusFlash(IntPtr gameHwnd, int virtualKey)
    {
        ushort scanCode = (ushort)NativeMethods.MapVirtualKeyW((uint)virtualKey, 0);

        FlashFocusAndExecute(gameHwnd, () =>
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)virtualKey;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYDOWN;

            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = (ushort)virtualKey;
            inputs[1].u.ki.wScan = scanCode;
            inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(1, new[] { inputs[0] }, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(_rng.Next(25, 55));
            NativeMethods.SendInput(1, new[] { inputs[1] }, Marshal.SizeOf<NativeMethods.INPUT>());
        });
    }

    private void SendInputClickViaFocusFlash(IntPtr gameHwnd, int clientX, int clientY)
    {
        var clientOrigin = new NativeMethods.POINT();
        NativeMethods.ClientToScreen(gameHwnd, ref clientOrigin);
        int screenX = clientOrigin.X + clientX;
        int screenY = clientOrigin.Y + clientY;

        NativeMethods.GetCursorPos(out var savedCursor);

        FlashFocusAndExecute(gameHwnd, () =>
        {
            NativeMethods.SetCursorPos(screenX, screenY);
            Thread.Sleep(_rng.Next(5, 12));

            var downInput = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_LEFTDOWN }
                }
            };
            NativeMethods.SendInput(1, new[] { downInput }, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(_rng.Next(25, 55));

            var upInput = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                u = new NativeMethods.INPUTUNION
                {
                    mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_LEFTUP }
                }
            };
            NativeMethods.SendInput(1, new[] { upInput }, Marshal.SizeOf<NativeMethods.INPUT>());
        });

        NativeMethods.SetCursorPos(savedCursor.X, savedCursor.Y);
    }

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
                if (myThreadId != fgThreadId)
                    attached = NativeMethods.AttachThreadInput(myThreadId, fgThreadId, true);

                NativeMethods.SetForegroundWindow(gameHwnd);
                Thread.Sleep(10);
                action();
                Thread.Sleep(5);
            }
            finally
            {
                if (previousFg != IntPtr.Zero && previousFg != gameHwnd)
                    NativeMethods.SetForegroundWindow(previousFg);
                if (attached)
                    NativeMethods.AttachThreadInput(myThreadId, fgThreadId, false);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  Window handle resolution — pure handle lookup, no
    //  attachment required for PostMessage path
    // ═══════════════════════════════════════════════════════════

    private IntPtr ResolveGameWindowHandle(ClientWindowRef window)
    {
        IntPtr candidateRoot = IntPtr.Zero;

        // 1. If the global attachment matches this target, prefer its handle
        //    (it may have been refreshed more recently).
        if (_attachmentService.IsAttached && _attachmentService.ProcessId == window.ProcessId)
        {
            IntPtr attachedHandle = _attachmentService.WindowHandle;
            if (attachedHandle != IntPtr.Zero && NativeMethods.IsWindow(attachedHandle))
                candidateRoot = attachedHandle;
        }

        // 2. Use the handle stored in the ClientWindowRef (set at bind time).
        if (candidateRoot == IntPtr.Zero)
        {
            IntPtr directHandle = new(window.WindowHandle);
            if (directHandle != IntPtr.Zero && NativeMethods.IsWindow(directHandle))
                candidateRoot = directHandle;
        }

        // 3. Last resort: enumerate top-level windows for the process.
        if (candidateRoot == IntPtr.Zero)
        {
            NativeMethods.EnumWindows((hWnd, _) =>
            {
                NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == (uint)window.ProcessId)
                {
                    candidateRoot = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
        }

        if (candidateRoot == IntPtr.Zero)
            return IntPtr.Zero;

        return ResolveBestInputHandle(candidateRoot, window.ProcessId);
    }

    private IntPtr ResolveBestInputHandle(IntPtr rootHandle, int expectedProcessId)
    {
        IntPtr bestChildHandle = IntPtr.Zero;
        int bestChildArea = -1;

        NativeMethods.EnumChildWindows(rootHandle, (hWnd, _) =>
        {
            if (!TryGetCandidateClientArea(hWnd, expectedProcessId, out int area))
                return true;

            if (area > bestChildArea)
            {
                bestChildArea = area;
                bestChildHandle = hWnd;
            }

            return true;
        }, IntPtr.Zero);

        if (bestChildHandle != IntPtr.Zero)
            return bestChildHandle;

        return TryGetCandidateClientArea(rootHandle, expectedProcessId, out _) ? rootHandle : IntPtr.Zero;
    }

    private static bool TryGetCandidateClientArea(IntPtr hWnd, int expectedProcessId, out int area)
    {
        area = 0;
        if (!NativeMethods.IsWindow(hWnd) || !NativeMethods.IsWindowVisible(hWnd))
            return false;

        NativeMethods.GetWindowThreadProcessId(hWnd, out uint processId);
        if (processId != (uint)expectedProcessId)
            return false;

        if (!NativeMethods.GetClientRect(hWnd, out var rect))
            return false;

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width < 64 || height < 64)
            return false;

        area = width * height;
        return true;
    }
}
