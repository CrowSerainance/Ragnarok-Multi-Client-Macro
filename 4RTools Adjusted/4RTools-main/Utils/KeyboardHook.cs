using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace _4RTools.Utils
{
    /// <summary>
    /// Detects hotkeys via GetAsyncKeyState polling (like YXExt.dll).
    /// Replaces SetWindowsHookEx(WH_KEYBOARD_LL) which Gepard Shield blocks.
    /// </summary>
    public static class KeyboardHook
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        /// <summary>Modifier states (updated each poll cycle).</summary>
        public static bool Control = false;
        public static bool Shift = false;
        public static bool Alt = false;
        public static bool Win = false;

        public delegate bool KeyPressed();

        private static Dictionary<Keys, KeyPressed> handledKeysDown = new Dictionary<Keys, KeyPressed>();
        private static Dictionary<Keys, KeyPressed> handledKeysUp = new Dictionary<Keys, KeyPressed>();

        public delegate bool KeyboardHookHandler(Keys key);
        public static KeyboardHookHandler KeyDown;

        private static volatile bool Enabled;
        private static Thread pollThread;
        private static volatile bool running;

        // Track previous key states for edge detection
        private static HashSet<int> keysCurrentlyDown = new HashSet<int>();

        private const int PollIntervalMs = 10; // ~100 Hz, matches YXExt

        public static bool Enable()
        {
            if (Enabled) return false;

            try
            {
                running = true;
                pollThread = new Thread(PollLoop)
                {
                    Name = "4RTools_HotkeyPoll",
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
                pollThread.Start();
                Enabled = true;
                return true;
            }
            catch
            {
                Enabled = false;
                return false;
            }
        }

        public static bool Disable()
        {
            if (!Enabled) return false;

            try
            {
                running = false;
                pollThread?.Join(2000);
                pollThread = null;
                Enabled = false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void PollLoop()
        {
            while (running)
            {
                // Update modifier states
                Control = IsKeyHeld(Keys.LControlKey) || IsKeyHeld(Keys.RControlKey);
                Shift = IsKeyHeld(Keys.LShiftKey) || IsKeyHeld(Keys.RShiftKey);
                Alt = IsKeyHeld(Keys.LMenu) || IsKeyHeld(Keys.RMenu);
                Win = IsKeyHeld(Keys.LWin) || IsKeyHeld(Keys.RWin);

                // Check all registered keys
                // Copy to array to avoid modification during iteration
                Keys[] downKeys;
                Keys[] upKeys;
                lock (handledKeysDown)
                {
                    downKeys = new Keys[handledKeysDown.Count];
                    handledKeysDown.Keys.CopyTo(downKeys, 0);
                }
                lock (handledKeysUp)
                {
                    upKeys = new Keys[handledKeysUp.Count];
                    handledKeysUp.Keys.CopyTo(upKeys, 0);
                }

                foreach (Keys key in downKeys)
                {
                    int vk = (int)key;
                    bool isDown = IsKeyHeld(key);

                    if (isDown && !keysCurrentlyDown.Contains(vk))
                    {
                        // Rising edge — key just pressed
                        keysCurrentlyDown.Add(vk);
                        OnKeyDown(key);
                    }
                    else if (!isDown && keysCurrentlyDown.Contains(vk))
                    {
                        // Falling edge — key just released
                        keysCurrentlyDown.Remove(vk);
                    }
                }

                foreach (Keys key in upKeys)
                {
                    int vk = (int)key;
                    bool isDown = IsKeyHeld(key);

                    if (isDown && !keysCurrentlyDown.Contains(vk))
                    {
                        keysCurrentlyDown.Add(vk);
                    }
                    else if (!isDown && keysCurrentlyDown.Contains(vk))
                    {
                        keysCurrentlyDown.Remove(vk);
                        OnKeyUp(key);
                    }
                }

                // Also check for unregistered keys via KeyDown delegate
                if (KeyDown != null)
                {
                    // Poll function keys for the delegate (F1-F24)
                    for (int vk = 0x70; vk <= 0x87; vk++)
                    {
                        bool isDown = (GetAsyncKeyState(vk) & 0x8000) != 0;
                        if (isDown && !keysCurrentlyDown.Contains(vk))
                        {
                            keysCurrentlyDown.Add(vk);
                            KeyDown((Keys)vk);
                        }
                        else if (!isDown && keysCurrentlyDown.Contains(vk))
                        {
                            keysCurrentlyDown.Remove(vk);
                        }
                    }
                }

                Thread.Sleep(PollIntervalMs);
            }
        }

        private static bool IsKeyHeld(Keys key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        public static bool AddKeyDown(Keys key, KeyPressed callback)
        {
            KeyDown = null;
            lock (handledKeysDown)
            {
                if (!handledKeysDown.ContainsKey(key))
                {
                    handledKeysDown.Add(key, callback);
                    return true;
                }
            }
            return false;
        }

        public static bool AddKeyUp(Keys key, KeyPressed callback)
        {
            lock (handledKeysUp)
            {
                if (!handledKeysUp.ContainsKey(key))
                {
                    handledKeysUp.Add(key, callback);
                    return true;
                }
            }
            return false;
        }

        public static bool Add(Keys key, KeyPressed callback)
        {
            return AddKeyDown(key, callback);
        }

        public static bool RemoveDown(Keys key)
        {
            lock (handledKeysDown)
            {
                return handledKeysDown.Remove(key);
            }
        }

        public static bool RemoveUp(Keys key)
        {
            lock (handledKeysUp)
            {
                return handledKeysUp.Remove(key);
            }
        }

        public static bool Remove(Keys key)
        {
            return RemoveDown(key);
        }

        private static bool OnKeyDown(Keys key)
        {
            if (KeyDown != null)
                return KeyDown(key);

            KeyPressed callback;
            lock (handledKeysDown)
            {
                if (handledKeysDown.TryGetValue(key, out callback))
                    return callback();
            }
            return true;
        }

        private static bool OnKeyUp(Keys key)
        {
            KeyPressed callback;
            lock (handledKeysUp)
            {
                if (handledKeysUp.TryGetValue(key, out callback))
                    return callback();
            }
            return true;
        }

        public static string KeyToString(Keys key)
        {
            return (Control ? "Ctrl + " : "") +
                   (Alt ? "Alt + " : "") +
                   (Shift ? "Shift + " : "") +
                   (Win ? "Win + " : "") +
                   key.ToString();
        }
    }
}
