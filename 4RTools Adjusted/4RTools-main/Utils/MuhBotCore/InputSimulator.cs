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
    private IntPtr WindowHandle => _processManager.GetGameWindowOrNull();

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
    /// </summary>
    public void SendKey(int vkCode, bool noDelay = false)
    {
        PostKeyDown(vkCode);
        if (noDelay)
            Thread.Sleep(_rng.Next(8, 15));
        else
            Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        PostKeyUp(vkCode);
    }

    /// <summary>
    /// Synchronous key sending using SendMessage.
    /// </summary>
    public void SendKeySendMessage(int vkCode)
    {
        uint scanCode = Native.MapVirtualKeyW((uint)vkCode, 0);
        uint lParamDown = 0x00000001 | (scanCode << 16);
        uint lParamUp   = 0xC0000001 | (scanCode << 16);

        Native.SendMessage(WindowHandle, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)lParamDown);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.SendMessage(WindowHandle, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)lParamUp);
    }

    /// <summary>
    /// Send Ctrl + key combination to the game window (background-safe via PostMessage).
    /// </summary>
    public void SendCtrlCombo(int vkCode)
    {
        const int ctrlVk = 0x11; // VK_CONTROL

        PostKeyDown(ctrlVk);
        Thread.Sleep(_rng.Next(15, 40));
        PostKeyDown(vkCode);
        Thread.Sleep(_rng.Next(20, 45));
        PostKeyUp(vkCode);
        Thread.Sleep(_rng.Next(15, 35));
        PostKeyUp(ctrlVk);
    }

    public void SendKeyChord(int vkCode, bool ctrl, bool alt, bool shift, bool win, bool noDelay = false)
    {
        int[] modifiers = BuildModifierList(ctrl, alt, shift, win);
        foreach (int modifier in modifiers)
        {
            PostKeyDown(modifier);
            Thread.Sleep(_rng.Next(5, 12));
        }

        PostKeyDown(vkCode);
        if (noDelay)
            Thread.Sleep(_rng.Next(8, 15));
        else
            Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        PostKeyUp(vkCode);

        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            Thread.Sleep(_rng.Next(5, 12));
            PostKeyUp(modifiers[i]);
        }
    }

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
        uint scanCode = Native.MapVirtualKeyW(0x32, 0); // '2' key scan code (@ = Shift+2)
        Native.PostMessage(WindowHandle, Native.WM_CHAR, (IntPtr)0x40, (IntPtr)(0x00000001 | (scanCode << 16)));
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
    /// <summary>
    /// Background left click using client-area coordinates.
    /// Sends direct virtual clicks to the game window's message queue via PostMessage.
    /// Does not steal OS focus or move the physical hardware mouse.
    /// </summary>
    public void SendClick(int x, int y)
    {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        Native.PostMessage(WindowHandle, Native.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Thread.Sleep(_rng.Next(8, 18));
        Native.PostMessage(WindowHandle, Native.WM_LBUTTONDOWN, (IntPtr)Native.MK_LBUTTON, lParam);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.PostMessage(WindowHandle, Native.WM_LBUTTONUP, IntPtr.Zero, lParam);
    }

    /// <summary>
    /// Synchronous background click using SendMessage. 
    /// Waits for the game window to process the click before returning.
    /// </summary>
    public void SendClickWithSendMessage(int x, int y)
    {
        IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
        Native.SendMessage(WindowHandle, Native.WM_MOUSEMOVE, IntPtr.Zero, lParam);
        Native.SendMessage(WindowHandle, Native.WM_LBUTTONDOWN, (IntPtr)Native.MK_LBUTTON, lParam);
        Thread.Sleep(GaussianDelay(55, 15, 25, 100));
        Native.SendMessage(WindowHandle, Native.WM_LBUTTONUP, IntPtr.Zero, lParam);
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
            // PostMessage-only flick: send a mouse-move to offset position first
            IntPtr flickParam = (IntPtr)(((screenY - 1) << 16) | ((screenX - 1) & 0xFFFF));
            Native.PostMessage(WindowHandle, Native.WM_MOUSEMOVE, IntPtr.Zero, flickParam);
            Thread.Sleep(_rng.Next(8, 18));
        }
        ClickAt(screenX, screenY);
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
            return false;
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

    private static uint BuildKeyDownLParam(int vkCode)
    {
        uint scanCode = Native.MapVirtualKeyW((uint)vkCode, 0);
        return 0x00000001 | (scanCode << 16);
    }

    private static uint BuildKeyUpLParam(int vkCode)
    {
        uint scanCode = Native.MapVirtualKeyW((uint)vkCode, 0);
        return 0xC0000001 | (scanCode << 16);
    }

    private void PostKeyDown(int vkCode)
    {
        Native.PostMessage(WindowHandle, Native.WM_KEYDOWN, (IntPtr)vkCode, (IntPtr)BuildKeyDownLParam(vkCode));
    }

    private void PostKeyUp(int vkCode)
    {
        Native.PostMessage(WindowHandle, Native.WM_KEYUP, (IntPtr)vkCode, (IntPtr)BuildKeyUpLParam(vkCode));
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

