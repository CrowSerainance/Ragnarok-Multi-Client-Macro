using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// PostMessage keyboard/mouse and memory-write mouse position. Works when game window is not focused.
/// </summary>
public class InputSimulator
{
    private readonly ProcessManager _processManager;
    private readonly MemoryReader _memory;
    private readonly int _mousePosXOffset;
    private readonly int _mousePosYOffset;
    private readonly Random _rng = new();

    /// <summary>
    /// Cached window handle, refreshed each operation. Avoids repeated GetGameWindowOrNull() calls
    /// within a single SendKey/SendClick sequence, which could return different handles if the
    /// window closes mid-operation (leaving keys stuck down or clicks orphaned).
    /// </summary>
    private IntPtr _cachedHwnd = IntPtr.Zero;

    /// <summary>Resolve and cache the game window handle. Call once at the start of each operation.</summary>
    private IntPtr ResolveWindowHandle()
    {
        _cachedHwnd = _processManager.GetGameWindowOrNull();
        return _cachedHwnd;
    }

    /// <summary>Returns the most recently resolved window handle (fast, no re-query).</summary>
    private IntPtr WindowHandle => _cachedHwnd != IntPtr.Zero ? _cachedHwnd : ResolveWindowHandle();

    public InputSimulator(ProcessManager processManager, MemoryReader memory, int mousePosXOffset, int mousePosYOffset)
    {
        _processManager = processManager;
        _memory = memory;
        _mousePosXOffset = mousePosXOffset;
        _mousePosYOffset = mousePosYOffset;
    }

    private static int Clamp(int value, int min, int max)
    {
        return (value < min) ? min : (value > max) ? max : value;
    }

    /// <summary>Gaussian-distributed delay (Box-Muller transform), clamped to [min, max].</summary>
    private int GaussianDelay(int mean, int stddev, int min, int max)
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = _rng.NextDouble();
        double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return Clamp((int)(mean + z * stddev), min, max);
    }

    /// <summary>
    /// Send key down and key up with random delay (anti-detection). Use noDelay: true for NDL mode (minimal delay).
    /// Resolves window handle once so both down and up go to the same target.
    /// </summary>
    public void SendKey(int vkCode, bool noDelay = false)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        uint lParamDown = BuildKeyDownLParam(vkCode);
        uint lParamUp = BuildKeyUpLParam(vkCode);

        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)lParamDown);
        if (noDelay)
            Thread.Sleep(_rng.Next(8, 15));
        else
            Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)lParamUp);
    }

    /// <summary>
    /// Synchronous key sending using SendMessage.
    /// </summary>
    public void SendKeySendMessage(int vkCode)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        uint lParamDown = BuildKeyDownLParam(vkCode);
        uint lParamUp = BuildKeyUpLParam(vkCode);

        Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)lParamDown);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)lParamUp);
    }

    /// <summary>
    /// Send Ctrl + key combination to the game window (background-safe via PostMessage).
    /// </summary>
    public void SendCtrlCombo(int vkCode)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        const int ctrlVk = 0x11; // VK_CONTROL

        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)ctrlVk, (IntPtr)BuildKeyDownLParam(ctrlVk));
        Thread.Sleep(_rng.Next(15, 40));
        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
        Thread.Sleep(_rng.Next(20, 45));
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));
        Thread.Sleep(_rng.Next(15, 35));
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)ctrlVk, (IntPtr)BuildKeyUpLParam(ctrlVk));
    }

    public void SendKeyChord(int vkCode, bool ctrl, bool alt, bool shift, bool win, bool noDelay = false)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        foreach (int modifier in modifiers)
        {
            Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)modifier, (IntPtr)BuildKeyDownLParam(modifier));
            Thread.Sleep(_rng.Next(5, 12));
        }

        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
        if (noDelay)
            Thread.Sleep(_rng.Next(8, 15));
        else
            Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(_rng.Next(5, 12));
            Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)modifiers[i], (IntPtr)BuildKeyUpLParam(modifiers[i]));
        }
    }

    public void SendKeyChordSendMessage(int vkCode, bool ctrl, bool alt, bool shift, bool win)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        foreach (int modifier in modifiers)
        {
            Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)modifier, (IntPtr)BuildKeyDownLParam(modifier));
            Thread.Sleep(_rng.Next(5, 12));
        }

        Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(_rng.Next(5, 12));
            Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)modifiers[i], (IntPtr)BuildKeyUpLParam(modifiers[i]));
        }
    }

    /// <summary>Resolve and cache game HWND; call once before a multi-key skill macro.</summary>
    public bool TryBeginSkillSequenceWindow() => ResolveWindowHandle() != IntPtr.Zero;

    /// <summary>
    /// Skill macro: synchronous <see cref="Native.SendMessage"/> so the window processes each key before the next.
    /// Does not call SetForegroundWindow — the client stays in the background.
    /// </summary>
    public void SendKeyTapSendMessageForSequence(int vkCode, int holdMs)
    {
        IntPtr hWnd = WindowHandle;
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        uint lParamDown = BuildKeyDownLParam(vkCode);
        uint lParamUp = BuildKeyUpLParam(vkCode);
        Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)lParamDown);
        Thread.Sleep(Math.Max(12, holdMs));
        Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)lParamUp);
    }

    /// <summary>Modifier chord for skill macro; same synchronous delivery as <see cref="SendKeyTapSendMessageForSequence"/>.</summary>
    public void SendKeyChordSendMessageForSequence(int vkCode, bool ctrl, bool alt, bool shift, bool win, int holdMs)
    {
        IntPtr hWnd = WindowHandle;
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        foreach (int modifier in modifiers)
        {
            Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)modifier, (IntPtr)BuildKeyDownLParam(modifier));
            Thread.Sleep(_rng.Next(8, 18));
        }

        Native.SendMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
        Thread.Sleep(Math.Max(12, holdMs));
        Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(_rng.Next(8, 16));
            Native.SendMessage(hWnd, Native.WM_KEYUP, (IntPtr)modifiers[i], (IntPtr)BuildKeyUpLParam(modifiers[i]));
        }
    }

    #region Skill Spammer — SendInput (foreground)

    private static int InputStructSize => Marshal.SizeOf(typeof(Native.INPUT));

    private static Native.INPUT KeyboardInput(ushort vk, uint extraFlags, bool keyUp)
    {
        uint flags = (keyUp ? Native.KEYEVENTF_KEYUP : 0) | extraFlags;
        return new Native.INPUT
        {
            type = Native.INPUT_KEYBOARD,
            U = new Native.INPUTUNION
            {
                ki = new Native.KEYBDINPUT
                {
                    wVk = vk,
                    wScan = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    private static Native.INPUT KeyboardInputScan(ushort scan, bool extended, bool keyUp)
    {
        uint flags = Native.KEYEVENTF_SCANCODE;
        if (extended)
        {
            flags |= Native.KEYEVENTF_EXTENDEDKEY;
        }

        if (keyUp)
        {
            flags |= Native.KEYEVENTF_KEYUP;
        }

        return new Native.INPUT
        {
            type = Native.INPUT_KEYBOARD,
            U = new Native.INPUTUNION
            {
                ki = new Native.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scan,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    /// <summary>Same extended cluster as WM_KEY lParam (SendInput KEYEVENTF_EXTENDEDKEY).</summary>
    private static uint ExtendedKeyEventFlags(ushort vk)
    {
        switch (vk)
        {
            case 0x21:
            case 0x22:
            case 0x23:
            case 0x24:
            case 0x25:
            case 0x26:
            case 0x27:
            case 0x28:
            case 0x2D:
            case 0x2E:
            case 0x5D:
            case 0x6F:
                return Native.KEYEVENTF_EXTENDEDKEY;
            default:
                return 0;
        }
    }

    private static uint SendInput1(Native.INPUT input)
    {
        return Native.SendInput(1, new[] { input }, InputStructSize);
    }

    private static bool SendKeyDownSendInput(ushort vk)
    {
        bool extK = ExtendedKeyEventFlags(vk) != 0;
        ushort scan = (ushort)(Native.MapVirtualKeyW(vk, Native.MAPVK_VK_TO_VSC) & 0xFFu);
        if (scan != 0 && SendInput1(KeyboardInputScan(scan, extK, keyUp: false)) != 0)
        {
            return true;
        }

        return SendInput1(KeyboardInput(vk, ExtendedKeyEventFlags(vk), keyUp: false)) != 0;
    }

    private static void SendKeyUpSendInput(ushort vk)
    {
        bool extK = ExtendedKeyEventFlags(vk) != 0;
        ushort scan = (ushort)(Native.MapVirtualKeyW(vk, Native.MAPVK_VK_TO_VSC) & 0xFFu);
        if (scan != 0)
        {
            SendInput1(KeyboardInputScan(scan, extK, keyUp: true));
        }
        else
        {
            SendInput1(KeyboardInput(vk, ExtendedKeyEventFlags(vk), keyUp: true));
        }
    }

    private bool TryTapVkSendInput(ushort vk, int holdMs)
    {
        bool extK = ExtendedKeyEventFlags(vk) != 0;
        ushort scan = (ushort)(Native.MapVirtualKeyW(vk, Native.MAPVK_VK_TO_VSC) & 0xFFu);
        if (scan != 0)
        {
            if (SendInput1(KeyboardInputScan(scan, extK, keyUp: false)) != 0)
            {
                Thread.Sleep(Math.Max(12, holdMs));
                if (SendInput1(KeyboardInputScan(scan, extK, keyUp: true)) != 0)
                {
                    return true;
                }
            }
        }

        uint ext = ExtendedKeyEventFlags(vk);
        if (SendInput1(KeyboardInput(vk, ext, keyUp: false)) != 0)
        {
            Thread.Sleep(Math.Max(12, holdMs));
            if (SendInput1(KeyboardInput(vk, ext, keyUp: true)) != 0)
            {
                return true;
            }

            IntPtr hWnd = WindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vk, (IntPtr)BuildKeyUpLParam(vk));
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// One key down/up via SendInput (scan-code first, then VK). Requires foreground; falls back to
    /// <see cref="SendKeyTapSendMessageForSequence"/> if both fail.
    /// </summary>
    public void SendKeyTapSendInputForSkillSpammer(int vkCode, int holdMs)
    {
        ushort vk = (ushort)(vkCode & 0xFFFF);
        if (TryTapVkSendInput(vk, holdMs))
        {
            return;
        }

        SendKeyTapSendMessageForSequence(vkCode, holdMs);
    }

    /// <summary>Modifier chord via SendInput; falls back to <see cref="SendKeyChordSendMessageForSequence"/>.</summary>
    public void SendKeyChordSendInputForSkillSpammer(int vkCode, bool ctrl, bool alt, bool shift, bool win, int holdMs)
    {
        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        ushort vk = (ushort)(vkCode & 0xFFFF);

        for (int mi = 0; mi < modifiers.Length; mi++)
        {
            ushort m = (ushort)modifiers[mi];
            if (!SendKeyDownSendInput(m))
            {
                for (int r = mi - 1; r >= 0; r--)
                {
                    SendKeyUpSendInput((ushort)modifiers[r]);
                }

                SendKeyChordSendMessageForSequence(vkCode, ctrl, alt, shift, win, holdMs);
                return;
            }

            Thread.Sleep(_rng.Next(10, 20));
        }

        if (!SendKeyDownSendInput(vk))
        {
            for (int r = modifiers.Length - 1; r >= 0; r--)
            {
                SendKeyUpSendInput((ushort)modifiers[r]);
            }

            SendKeyChordSendMessageForSequence(vkCode, ctrl, alt, shift, win, holdMs);
            return;
        }

        Thread.Sleep(Math.Max(12, holdMs));
        SendKeyUpSendInput(vk);

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(_rng.Next(8, 16));
            SendKeyUpSendInput((ushort)modifiers[i]);
        }
    }

    /// <summary>
    /// Stronger foreground for SendInput: restore, AttachThreadInput from foreground thread, BringWindowToTop.
    /// </summary>
    public bool TryFocusGameWindowForSkillMacro()
    {
        IntPtr target = WindowHandle;
        if (target == IntPtr.Zero)
        {
            return false;
        }

        Native.ShowWindow(target, Native.SW_RESTORE);

        if (Native.GetForegroundWindow() == target)
        {
            return true;
        }

        uint thisId = Native.GetCurrentThreadId();
        IntPtr fgWnd = Native.GetForegroundWindow();
        uint fgThread = fgWnd != IntPtr.Zero ? Native.GetWindowThreadProcessId(fgWnd, out _) : 0;

        if (fgThread != 0 && fgThread != thisId)
        {
            Native.AttachThreadInput(thisId, fgThread, true);
        }

        Native.BringWindowToTop(target);
        bool ok = Native.SetForegroundWindow(target);

        if (fgThread != 0 && fgThread != thisId)
        {
            Native.AttachThreadInput(thisId, fgThread, false);
        }

        if (ok || Native.GetForegroundWindow() == target)
        {
            return true;
        }

        uint targetThread = Native.GetWindowThreadProcessId(target, out _);
        if (targetThread != 0 && targetThread != thisId)
        {
            Native.AttachThreadInput(thisId, targetThread, true);
            Native.SetForegroundWindow(target);
            Native.AttachThreadInput(thisId, targetThread, false);
        }

        return Native.GetForegroundWindow() == target;
    }

    #endregion

    public void SendKey(char key)
    {
        short vk = Native.VkKeyScanW(key);
        if (vk == -1) return; // no mapping for this character
        int vkCode = vk & 0xFF;
        SendKey(vkCode);
    }

    /// <summary>
    /// Send @ via WM_CHAR (as in pybot).
    /// </summary>
    public void SendAtChar()
    {
        IntPtr hWnd = _cachedHwnd != IntPtr.Zero ? _cachedHwnd : ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;
        uint scanCode = Native.MapVirtualKeyW(0x32, 0); // '2' key scan code (@ = Shift+2)
        Native.PostMessage(hWnd, Native.WM_CHAR, (IntPtr)0x40, (IntPtr)(0x00000001 | (scanCode << 16)));
    }

    /// <summary>
    /// Type string: @ via WM_CHAR, others via VkKeyScan + key down/up, then Enter.
    /// </summary>
    public void SendChatCommand(string text)
    {
        foreach (char c in text)
        {
            if (c == '@')
            {
                SendAtChar();
            }
            else
            {
                short vk = Native.VkKeyScanW(c);
                if (vk != -1)
                    SendKey(vk & 0xFF);
            }
            Thread.Sleep(GaussianDelay(35, 10, 15, 70));
        }
        Thread.Sleep(GaussianDelay(100, 25, 50, 200));
        SendKey(0x0D); // Enter
    }

    /// <summary>
    /// Open chat bar (Enter), type command, press Enter.
    /// </summary>
    public bool SendChatCommandWithEnter(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        // Block GM-style commands to keep automation in regular-player behavior.
        if (command.TrimStart().StartsWith("@", StringComparison.Ordinal))
            return false;

        SendKey(0x0D); // Enter to open chat
        Thread.Sleep(GaussianDelay(300, 60, 150, 500));
        SendChatCommand(command);
        return true;
    }

    /// <summary>
    /// Set game's internal mouse position (memory write). Uses client-area coordinates.
    /// </summary>
    public void SetMousePosition(int screenX, int screenY)
    {
        IntPtr baseAddr = _memory.BaseAddress;
        _memory.WriteUInt32(IntPtr.Add(baseAddr, _mousePosXOffset), (uint)screenX);
        _memory.WriteUInt32(IntPtr.Add(baseAddr, _mousePosYOffset), (uint)screenY);
    }

    /// <summary>
    /// Background left click using client-area coordinates.
    /// Sends direct virtual clicks to the game window's message queue via PostMessage.
    /// Does not steal OS focus or move the physical hardware mouse.
    /// </summary>
    public void SendClick(int x, int y)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        Native.PostMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Thread.Sleep(_rng.Next(8, 18));
        Native.PostMessage(hWnd, Native.WM_LBUTTONDOWN, (IntPtr)Native.MK_LBUTTON, lParam);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.PostMessage(hWnd, Native.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }

    /// <summary>
    /// Fast background left click for high-speed skill spamming.
    /// Uses minimal random delays (2-8ms) instead of the default Gaussian delay.
    /// </summary>
    public void PostClick(int x, int y)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        Native.PostMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Thread.Sleep(_rng.Next(2, 5));
        Native.PostMessage(hWnd, Native.WM_LBUTTONDOWN, (IntPtr)Native.MK_LBUTTON, lParam);
        Thread.Sleep(_rng.Next(4, 10));
        Native.PostMessage(hWnd, Native.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }

    /// <summary>
    /// Synchronous background click using SendMessage. 
    /// Waits for the game window to process the click before returning.
    /// </summary>
    public void SendClickWithSendMessage(int x, int y)
    {
        IntPtr hWnd = ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;

        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        Native.SendMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Native.SendMessage(hWnd, Native.WM_LBUTTONDOWN, (IntPtr)Native.MK_LBUTTON, lParam);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.SendMessage(hWnd, Native.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }





    /// <summary>
    /// Click at position with Gaussian jitter. Uses PostMessage only by default (safe for any server).
    /// When mouse offset addresses are configured (non-zero), also writes position to game memory for maximum realism.
    /// </summary>
    public void ClickAt(int screenX, int screenY)
    {
        var (finalX, finalY) = ApplyGaussianJitter(screenX, screenY);
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(finalX, finalY);
            SendClick(finalX, finalY);
            _processManager.CloseWriteHandle();
        }
        else
        {
            SendClick(finalX, finalY);
        }
    }

    /// <summary>
    /// Click at exact position without jitter.
    /// </summary>
    public void ClickAtExact(int screenX, int screenY)
    {
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(screenX, screenY);
            SendClick(screenX, screenY);
            _processManager.CloseWriteHandle();
        }
        else
        {
            SendClick(screenX, screenY);
        }
    }

    /// <summary>
    /// Click at exact position without jitter using SendMessage.
    /// </summary>
    public void ClickAtExactSendMessage(int screenX, int screenY)
    {
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(screenX, screenY);
            SendClickWithSendMessage(screenX, screenY);
            _processManager.CloseWriteHandle();
        }
        else
        {
            SendClickWithSendMessage(screenX, screenY);
        }
    }

    /// <summary>
    /// Click using PostMessage only (no memory write). Useful for anti-integrity environments.
    /// </summary>
    public void ClickAtNoMemoryWrite(int screenX, int screenY)
    {
        var (finalX, finalY) = ApplyGaussianJitter(screenX, screenY);
        SendClick(finalX, finalY);
    }

    /// <summary>
    /// Exact click using PostMessage only (no memory write).
    /// </summary>
    public void ClickAtExactNoMemoryWrite(int screenX, int screenY)
    {
        SendClick(screenX, screenY);
    }

    /// <summary>
    /// Click with mouse flick: 1-pixel diagonal offset before actual click.
    /// Prevents the game client from ignoring repeated same-position clicks
    /// on ground-targeted skills (the client deduplicates identical click coords).
    /// Reference: 4RTools AHK.cs mouseFlick feature.
    /// </summary>
    public void ClickAtWithFlick(int screenX, int screenY)
    {
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(screenX - 1, screenY - 1);
            Thread.Sleep(_rng.Next(8, 18));
        }
        else
        {
            IntPtr hWnd = ResolveWindowHandle();
            if (hWnd == IntPtr.Zero) return;
            IntPtr flickParam = (IntPtr)(((screenY - 1) << 16) | ((screenX - 1) & 0xFFFF));
            Native.PostMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, flickParam);
            Thread.Sleep(_rng.Next(8, 18));
        }
        ClickAt(screenX, screenY);
    }

    /// <summary>
    /// Exact click with mouse flick. Keeps the anti-dedup flick without adding Gaussian jitter.
    /// </summary>
    public void ClickAtExactWithFlick(int screenX, int screenY)
    {
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(screenX - 1, screenY - 1);
            Thread.Sleep(_rng.Next(8, 18));
            SetMousePosition(screenX, screenY);
            SendClick(screenX, screenY);
            _processManager.CloseWriteHandle();
        }
        else
        {
            IntPtr hWnd = ResolveWindowHandle();
            if (hWnd == IntPtr.Zero) return;

            IntPtr flickParam = (IntPtr)(((screenY - 1) << 16) | ((screenX - 1) & 0xFFFF));
            Native.PostMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, flickParam);
            Thread.Sleep(_rng.Next(8, 18));
            SendClick(screenX, screenY);
        }
    }

    /// <summary>
    /// Fast click with mouse flick for high-speed skill spamming.
    /// </summary>
    public void PostClickWithFlick(int screenX, int screenY)
    {
        if (_mousePosXOffset != 0 && _mousePosYOffset != 0)
        {
            SetMousePosition(screenX - 1, screenY - 1);
            Thread.Sleep(_rng.Next(2, 5));
        }
        else
        {
            IntPtr hWnd = ResolveWindowHandle();
            if (hWnd == IntPtr.Zero) return;
            IntPtr flickParam = (IntPtr)(((screenY - 1) << 16) | ((screenX - 1) & 0xFFFF));
            Native.PostMessage(hWnd, Native.WM_MOUSEMOVE, IntPtr.Zero, flickParam);
            Thread.Sleep(_rng.Next(2, 5));
        }
        PostClick(screenX, screenY);
    }

    public void ClickEngageTarget(int screenX, int screenY)
    {
        var points = new (int x, int y)[]
        {
            (screenX, screenY),
            (screenX, screenY - 10),
            (screenX + 10, screenY - 4),
            (screenX - 10, screenY - 4)
        };

        foreach (var point in points)
        {
            ClickAtExact(point.x, point.y);
            Thread.Sleep(_rng.Next(25, 45));
        }
    }

    public bool TryGetClientCenter(out int centerX, out int centerY)
    {
        centerX = 400;
        centerY = 300;

        if (WindowHandle == IntPtr.Zero)
            return false;

        if (!Native.GetClientRect(WindowHandle, out var rect))
            return false;

        centerX = Math.Max(1, (rect.Right - rect.Left) / 2);
        centerY = Math.Max(1, (rect.Bottom - rect.Top) / 2);
        return true;
    }

    public bool TryFocusGameWindow()
    {
        if (WindowHandle == IntPtr.Zero)
        {
            return false;
        }

        return Native.SetForegroundWindow(WindowHandle);
    }

    public bool TryConvertClientToScreen(int clientX, int clientY, out int screenX, out int screenY)
    {
        screenX = 0;
        screenY = 0;
        if (WindowHandle == IntPtr.Zero)
            return false;

        var point = new Native.POINT { X = clientX, Y = clientY };
        if (!Native.ClientToScreen(WindowHandle, ref point))
            return false;

        screenX = point.X;
        screenY = point.Y;
        return true;
    }

    private (int x, int y) ApplyGaussianJitter(int screenX, int screenY)
    {
        // 2D Gaussian jitter (stddev ~4px) instead of uniform +/-8px.
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = _rng.NextDouble();
        double z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        double z2 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        int jitterX = (int)Clamp((int)(z1 * 4.0), -12, 12);
        int jitterY = (int)Clamp((int)(z2 * 4.0), -12, 12);
        return (screenX + jitterX, screenY + jitterY);
    }

    /// <summary>WM_KEY* lParam bit 24 — extended keys per MSDN (arrows, home/end, ins/del, numpad /, etc.).</summary>
    private static uint ExtendedKeyLParamMask(int vkCode)
    {
        switch (vkCode)
        {
            case 0x21: // Prior
            case 0x22: // Next
            case 0x23: // End
            case 0x24: // Home
            case 0x25: // Left
            case 0x26: // Up
            case 0x27: // Right
            case 0x28: // Down
            case 0x2D: // Insert
            case 0x2E: // Delete
            case 0x5D: // Apps
            case 0x6F: // Divide (numpad)
                return 0x01000000u;
            default:
                return 0;
        }
    }

    private static uint BuildKeyDownLParam(int vkCode)
    {
        uint scanCode = Native.MapVirtualKeyW((uint)vkCode, 0);
        return 0x00000001 | (scanCode << 16) | ExtendedKeyLParamMask(vkCode);
    }

    private static uint BuildKeyUpLParam(int vkCode)
    {
        uint scanCode = Native.MapVirtualKeyW((uint)vkCode, 0);
        return 0xC0000001 | (scanCode << 16) | ExtendedKeyLParamMask(vkCode);
    }

    public void PostKeyDown(int vkCode)
    {
        IntPtr hWnd = _cachedHwnd != IntPtr.Zero ? _cachedHwnd : ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;
        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
    }

    public void PostKeyUp(int vkCode)
    {
        IntPtr hWnd = _cachedHwnd != IntPtr.Zero ? _cachedHwnd : ResolveWindowHandle();
        if (hWnd == IntPtr.Zero) return;
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));
    }

    /// <summary>
    /// Bind-slot macro: one full key cycle on a fixed HWND — WM_KEYDOWN, short hold, WM_KEYUP (correct lParam for RO).
    /// </summary>
    public void PostMacroKeyFullPress(IntPtr hWnd, int vkCode, int intraKeyHoldMs)
    {
        if (hWnd == IntPtr.Zero) return;
        int hold = Math.Max(5, Math.Min(15, intraKeyHoldMs));
        uint ld = BuildKeyDownLParam(vkCode);
        uint lu = BuildKeyUpLParam(vkCode);
        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)ld);
        Thread.Sleep(hold);
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)lu);
    }

    /// <summary>
    /// Chord: modifier keydowns, full main-key press, modifier keyups — same HWND for the whole step.
    /// </summary>
    public void PostMacroChordFullPress(IntPtr hWnd, int vkCode, bool ctrl, bool alt, bool shift, bool win, int intraKeyHoldMs)
    {
        if (hWnd == IntPtr.Zero) return;
        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        const int modGapMs = 5;
        foreach (int m in modifiers)
        {
            Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)m, (IntPtr)BuildKeyDownLParam(m));
            Thread.Sleep(modGapMs);
        }

        PostMacroKeyFullPress(hWnd, vkCode, intraKeyHoldMs);

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(modGapMs);
            int m = modifiers[i];
            Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)m, (IntPtr)BuildKeyUpLParam(m));
        }
    }

    /// <summary>
    /// rsm-master style: <c>PostMessage(WM_KEYDOWN/UP, vk, lParam: 0)</c> to a fixed HWND (same idea as
    /// <c>RSMForm.AHKThreadExecution</c> → <c>Interop.PostMessage(..., 0)</c>). Many RO clients accept this
    /// for chained skill keys where SendMessage + scan lParam does not.
    /// </summary>
    public void PostMacroKeyTapRsmStyle(IntPtr hWnd, int vkCode, int intraKeyHoldMs)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        int hold = Math.Max(5, Math.Min(25, intraKeyHoldMs));
        Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)vkCode, IntPtr.Zero);
        Thread.Sleep(hold);
        Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)vkCode, IntPtr.Zero);
    }

    /// <summary>Modifier chord with zero lParam on all posts (rsm-style delivery).</summary>
    public void PostMacroChordRsmStyle(IntPtr hWnd, int vkCode, bool ctrl, bool alt, bool shift, bool win, int intraKeyHoldMs)
    {
        if (hWnd == IntPtr.Zero)
        {
            return;
        }

        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        const int modGapMs = 5;
        foreach (int m in modifiers)
        {
            Native.PostMessage(hWnd, Native.WM_KEYDOWN, (IntPtr)m, IntPtr.Zero);
            Thread.Sleep(modGapMs);
        }

        PostMacroKeyTapRsmStyle(hWnd, vkCode, intraKeyHoldMs);

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(modGapMs);
            int m = modifiers[i];
            Native.PostMessage(hWnd, Native.WM_KEYUP, (IntPtr)m, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Bind-slot macro: one full key cycle via SendInput (same intent as <see cref="PostMacroKeyFullPress"/>).
    /// Uses scan-code path when available; requires foreground for reliable delivery.
    /// </summary>
    public void SendMacroKeyFullPressSendInput(int vkCode, int intraKeyHoldMs)
    {
        int hold = Math.Max(5, Math.Min(15, intraKeyHoldMs));
        TryTapVkSendInput((ushort)(vkCode & 0xFFFF), hold);
    }

    /// <summary>Bind-slot chord via SendInput (modifiers + main key), aligned with <see cref="PostMacroChordFullPress"/>.</summary>
    public void SendMacroChordFullPressSendInput(int vkCode, bool ctrl, bool alt, bool shift, bool win, int intraKeyHoldMs)
    {
        int hold = Math.Max(5, Math.Min(15, intraKeyHoldMs));
        SendKeyChordSendInputForSkillSpammer(vkCode, ctrl, alt, shift, win, hold);
    }

    private static int[] BuildModifierList(bool ctrl, bool alt, bool shift, bool win)
    {
        var modifiers = new System.Collections.Generic.List<int>(4);
        if (ctrl) modifiers.Add(0x11);
        if (alt) modifiers.Add(0x12);
        if (shift) modifiers.Add(0x10);
        if (win) modifiers.Add(0x5B);
        return modifiers.ToArray();
    }
}

