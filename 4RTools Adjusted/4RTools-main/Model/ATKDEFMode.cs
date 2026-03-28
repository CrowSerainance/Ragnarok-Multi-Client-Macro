using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;
using _4RTools.Utils.MuhBotCore;
using Newtonsoft.Json;

namespace _4RTools.Model
{
    public enum ATKDEFEnum
    {
        ATK = 0,
        DEF = 1,
    }

    public class ATKDEFMode : Action
    {
        public static string ACTION_NAME_ATKDEF = "ATKDEFMode";
        private _4RThread thread;
        public int ahkDelay { get; set; } = 10;
        public int switchDelay { get; set; } = 50;
        public Key keySpammer { get; set; }
        public bool keySpammerWithClick { get; set; } = true;
        public Dictionary<string, Key> defKeys { get; set; } = new Dictionary<string, Key>();
        public Dictionary<string, Key> atkKeys { get; set; } = new Dictionary<string, Key>();

        // ReSharper disable once NotAccessedField.Local - kept for potential future use
#pragma warning disable CS0414
        private int PX_MOV = Constants.MOUSE_DIAGONAL_MOVIMENTATION_PIXELS_AHK;
#pragma warning restore CS0414

        private bool IsWpfKeyDown(Key wpfKey)
        {
            Keys formsKey = toKeys(wpfKey);
            return (Native.GetAsyncKeyState((int)formsKey) & 0x8000) != 0;
        }

        public string GetActionName()
        {
            return ACTION_NAME_ATKDEF;
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread(_ => AHKThreadExecution(roClient));
                _4RThread.Start(this.thread);
            }
        }

        private int AHKThreadExecution(Client roClient)
        {
            if (this.keySpammer == Key.None)
            {
                return 0;
            }

            Keys thisk = toKeys(keySpammer);
            if (ProfileSingleton.GetCurrent().AHK.IsMainKeyClaimedByActiveSlot(thisk))
            {
                return 0;
            }

            if (IsWpfKeyDown(this.keySpammer))
            {
                if (InputAutomationStopProtocol.ShouldYieldBuffStyleInput())
                {
                    return 0;
                }

                InputAutomationStopProtocol.EnterExclusiveAutomation();
                try
                {
                    foreach (Key key in atkKeys.Values)
                    {
                        roClient.input.SendKey((int)toKeys(key), true);
                        Thread.Sleep(this.switchDelay);
                    }

                    while (IsThreadRunning() && IsWpfKeyDown(this.keySpammer))
                    {
                        roClient.input.SendKey((int)thisk, true);
                        if (this.keySpammerWithClick)
                        {
                            Native.POINT p;
                            Native.GetCursorPos(out p);
                            IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                            if (hWnd != IntPtr.Zero) Native.ScreenToClient(hWnd, ref p);
                            roClient.input.ClickAtExact(p.X, p.Y);
                        }

                        Thread.Sleep(this.ahkDelay);
                    }

                    foreach (Key key in defKeys.Values)
                    {
                        roClient.input.SendKey((int)toKeys(key), true);
                        Thread.Sleep(this.switchDelay);
                    }
                }
                finally
                {
                    InputAutomationStopProtocol.LeaveExclusiveAutomation();
                }
            }

            return 0;
        }

        public void AddSwitchItem(string dictKey, Key k, ATKDEFEnum type)
        {
            Dictionary<string, Key> copy = type == ATKDEFEnum.DEF ? this.defKeys : this.atkKeys;

            if (copy.ContainsKey(dictKey))
            {
                RemoveSwitchEntry(dictKey, type);
            }

            if (k != Key.None)
            {
                copy.Add(dictKey, k);
            }
        }

        public void RemoveSwitchEntry(string dictKey, ATKDEFEnum type)
        {
            Dictionary<string, Key> copy = type == ATKDEFEnum.DEF ? this.defKeys : this.atkKeys;
            copy.Remove(dictKey);
        }

        private Keys toKeys(Key k)
        {
            return (Keys)Enum.Parse(typeof(Keys), k.ToString());
        }

        private bool IsThreadRunning()
        {
            return this.thread != null && this.thread.IsRunning;
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
        }
    }
}
