using System.Runtime.InteropServices;
using PersonalRagnarokTool.Core.Models;

namespace PersonalRagnarokTool.Services;

public sealed class InputDispatcher : IDisposable
{
    private readonly Random _rng = new();
    private readonly object _focusLock = new();
    private readonly InterceptionService _interceptionService = new();

    private readonly record struct WindowTarget(IntPtr RootHandle, IntPtr InputHandle)
    {
        public bool IsValid => RootHandle != IntPtr.Zero && InputHandle != IntPtr.Zero;
    }

    public void Dispose()
    {
        _interceptionService.Dispose();
    }

    public bool SendKey(ClientWindowRef window, string? clientKey, InputMethod method, out string status)
    {
        status = string.Empty;
        if (string.IsNullOrWhiteSpace(clientKey))
            return true;

        if (!VirtualKeyMap.TryGetVirtualKey(clientKey, out int vk))
        {
            status = $"Unsupported key '{clientKey}'.";
            return false;
        }

        WindowTarget target = ResolveWindowTarget(window);
        if (!target.IsValid)
        {
            status = "No valid window handle.";
            return false;
        }

        if (method == InputMethod.Interception && _interceptionService.IsAvailable)
        {
            FlashFocusAndExecute(target, () => _interceptionService.SendKey(vk));
        }
        else if (method == InputMethod.PostMessage)
        {
            PostMessageKey(target.InputHandle, vk);
        }
        else
        {
            SendInputKeyViaFlash(target, vk);
        }

        status = $"{clientKey} sent via {method}.";
        return true;
    }

    public void SendClick(ClientWindowRef window, int clientX, int clientY, InputMethod method)
    {
        WindowTarget target = ResolveWindowTarget(window);
        if (!target.IsValid || !NativeMethods.IsWindow(target.InputHandle))
            return;

        if (method == InputMethod.Interception && _interceptionService.IsAvailable)
        {
            var clientOrigin = new NativeMethods.POINT();
            NativeMethods.ClientToScreen(target.InputHandle, ref clientOrigin);
            int screenX = clientOrigin.X + clientX;
            int screenY = clientOrigin.Y + clientY;

            NativeMethods.GetCursorPos(out var savedCursor);

            FlashFocusAndExecute(target, () =>
            {
                _interceptionService.SendClick(screenX, screenY);
            });

            NativeMethods.SetCursorPos(savedCursor.X, savedCursor.Y);
        }
        else if (method == InputMethod.PostMessage)
        {
            PostMessageClick(target.InputHandle, clientX, clientY);
        }
        else
        {
            SendInputClickViaFlash(target, clientX, clientY);
        }
    }

    private void PostMessageKey(IntPtr hwnd, int vk)
    {
        uint scanCode = NativeMethods.MapVirtualKeyW((uint)vk, 0);
        IntPtr downLParam = (IntPtr)(1 | (scanCode << 16));
        IntPtr upLParam = (IntPtr)(1 | (scanCode << 16) | (1 << 30) | (1 << 31));

        NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYDOWN, (IntPtr)vk, downLParam);
        Thread.Sleep(_rng.Next(25, 55));
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_KEYUP, (IntPtr)vk, upLParam);
    }

    private static void PostMessageClick(IntPtr hwnd, int x, int y)
    {
        IntPtr lParam = MakeLParam(x, y);
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }

    private static IntPtr MakeLParam(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private void SendInputKeyViaFlash(WindowTarget target, int vk)
    {
        ushort scanCode = (ushort)NativeMethods.MapVirtualKeyW((uint)vk, 0);

        FlashFocusAndExecute(target, () =>
        {
            var inputs = new NativeMethods.INPUT[2];

            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = (ushort)vk;
            inputs[0].u.ki.wScan = scanCode;
            inputs[0].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYDOWN;

            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = (ushort)vk;
            inputs[1].u.ki.wScan = scanCode;
            inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(1, new[] { inputs[0] }, Marshal.SizeOf<NativeMethods.INPUT>());
            Thread.Sleep(_rng.Next(25, 55));
            NativeMethods.SendInput(1, new[] { inputs[1] }, Marshal.SizeOf<NativeMethods.INPUT>());
        });
    }

    private void SendInputClickViaFlash(WindowTarget target, int clientX, int clientY)
    {
        var clientOrigin = new NativeMethods.POINT();
        NativeMethods.ClientToScreen(target.InputHandle, ref clientOrigin);
        int screenX = clientOrigin.X + clientX;
        int screenY = clientOrigin.Y + clientY;

        NativeMethods.GetCursorPos(out var savedCursor);

        FlashFocusAndExecute(target, () =>
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

    private void FlashFocusAndExecute(WindowTarget target, Action action)
    {
        lock (_focusLock)
        {
            IntPtr previousForeground = NativeMethods.GetForegroundWindow();
            uint currentThreadId = NativeMethods.GetCurrentThreadId();
            uint foregroundThreadId = NativeMethods.GetWindowThreadProcessId(previousForeground, out _);
            uint targetThreadId = NativeMethods.GetWindowThreadProcessId(target.RootHandle, out _);
            bool attachedToForeground = false;
            bool attachedToTarget = false;

            try
            {
                if (foregroundThreadId != 0 && currentThreadId != foregroundThreadId)
                    attachedToForeground = NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);

                if (targetThreadId != 0 && currentThreadId != targetThreadId)
                    attachedToTarget = NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, true);

                if (NativeMethods.IsIconic(target.RootHandle))
                    NativeMethods.ShowWindow(target.RootHandle, NativeMethods.SW_RESTORE);

                NativeMethods.BringWindowToTop(target.RootHandle);
                NativeMethods.SetForegroundWindow(target.RootHandle);
                NativeMethods.SetActiveWindow(target.RootHandle);
                if (target.InputHandle != IntPtr.Zero)
                    NativeMethods.SetFocus(target.InputHandle);

                Thread.Sleep(15);
                action();
                Thread.Sleep(5);
            }
            finally
            {
                if (previousForeground != IntPtr.Zero && previousForeground != target.RootHandle)
                {
                    NativeMethods.BringWindowToTop(previousForeground);
                    NativeMethods.SetForegroundWindow(previousForeground);
                }

                if (attachedToTarget)
                    NativeMethods.AttachThreadInput(currentThreadId, targetThreadId, false);

                if (attachedToForeground)
                    NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    private WindowTarget ResolveWindowTarget(ClientWindowRef window)
    {
        IntPtr candidateRoot = IntPtr.Zero;

        IntPtr directHandle = new(window.WindowHandle);
        if (directHandle != IntPtr.Zero && NativeMethods.IsWindow(directHandle))
            candidateRoot = directHandle;

        if (candidateRoot == IntPtr.Zero && window.ProcessId > 0)
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
            return default;

        IntPtr rootHandle = NativeMethods.GetAncestor(candidateRoot, NativeMethods.GA_ROOT);
        if (rootHandle == IntPtr.Zero)
            rootHandle = candidateRoot;

        IntPtr inputHandle = ResolveBestInputHandle(rootHandle, window.ProcessId);
        if (inputHandle == IntPtr.Zero)
            inputHandle = rootHandle;

        return new WindowTarget(rootHandle, inputHandle);
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
