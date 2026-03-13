using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace PersonalRagnarokTool.Services;

public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private readonly NativeMethods.HookProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private readonly HotkeyDispatcher _dispatcher;

    public GlobalKeyboardHook(HotkeyDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _proc = HookCallback;
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule != null)
            {
                _hookID = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var key = KeyInterop.KeyFromVirtualKey(vkCode);

            if (_dispatcher.IsKeyRegistered(key))
            {
                _dispatcher.DispatchKey(key);
                return (IntPtr)1; // Consume the keypress
            }
        }
        return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookID != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }
}
