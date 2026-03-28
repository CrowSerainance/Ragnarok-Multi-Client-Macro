using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;
using _4RTools.Utils.MuhBotCore;
using Newtonsoft.Json;

namespace _4RTools.Model
{
    public class MacroKey
    {
        public Key key { get; set; }
        public int delay { get; set; } = 50;
        public bool hasClick { get; set; } = false;

        public MacroKey(Key key, int delay)
        {
            this.key = key;
            this.delay = delay;
        }
    }

    public class ChainConfig
    {
        public int id;
        public Key trigger { get; set; }
        public Key daggerKey { get; set; }
        public Key instrumentKey { get; set; }
        public int delay { get; set; } = 50;
        public Dictionary<string, MacroKey> macroEntries { get; set; } = new Dictionary<string, MacroKey>();
        public bool infinityLoop { get; set; } = false;
        public bool infinityLoopOn { get; set; } = false;

        [JsonIgnore]
        public int currentStep { get; set; } = 0;

        public ChainConfig() { }

        public ChainConfig(int id)
        {
            this.id = id;
            this.macroEntries = new Dictionary<string, MacroKey>();
        }

        public ChainConfig(ChainConfig macro)
        {
            this.id = macro.id;
            this.delay = macro.delay;
            this.trigger = macro.trigger;
            this.daggerKey = macro.daggerKey;
            this.instrumentKey = macro.instrumentKey;
            this.infinityLoop = macro.infinityLoop;
            this.macroEntries = new Dictionary<string, MacroKey>(macro.macroEntries);
        }

        public ChainConfig(int id, Key trigger)
        {
            this.id = id;
            this.trigger = trigger;
            this.macroEntries = new Dictionary<string, MacroKey>();
        }
    }

    public class Macro : Action
    {
        public static string ACTION_NAME_SONG_MACRO = "SongMacro2.0";
        public static string ACTION_NAME_MACRO_SWITCH = "MacroSwitch2.0";

        private static bool IsWpfKeyDown(Key wpfKey)
        {
            Keys formsKey = (Keys)Enum.Parse(typeof(Keys), wpfKey.ToString());
            return (Native.GetAsyncKeyState((int)formsKey) & 0x8000) != 0;
        }

        private static Keys ToFormsKey(Key wpfKey)
        {
            if (wpfKey == Key.None)
            {
                return Keys.None;
            }

            return (Keys)Enum.Parse(typeof(Keys), wpfKey.ToString());
        }

        public string actionName { get; set; }
        private _4RThread thread;
        public List<ChainConfig> chainConfigs { get; set; } = new List<ChainConfig>();

        /// <summary>
        /// Tracks which trigger keys are currently held so the chain fires once per press
        /// (rising-edge detection) instead of repeating every poll cycle.
        /// </summary>
        private readonly HashSet<Key> _triggersDown = new HashSet<Key>();

        public Macro(string macroname, int macroLanes)
        {
            this.actionName = macroname;
            for (int i = 1; i <= macroLanes; i++)
            {
                chainConfigs.Add(new ChainConfig(i, Key.None));
            }
        }

        public void ResetMacro(int macroId)
        {
            try
            {
                chainConfigs[macroId - 1] = new ChainConfig(macroId);
            }
            catch (Exception) { }
        }

        public string GetActionName()
        {
            return this.actionName;
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        private int MacroExecutionThread(Client roClient)
        {
            bool anyTriggered = false;

            foreach (ChainConfig chainConfig in this.chainConfigs)
            {
                Keys triggerKey = ToFormsKey(chainConfig.trigger);
                if (triggerKey == Keys.None)
                {
                    continue;
                }

                if (ProfileSingleton.GetCurrent().AHK.IsMainKeyClaimedByActiveSlot(triggerKey))
                {
                    continue;
                }

                bool isDown = IsWpfKeyDown(chainConfig.trigger);

                if (isDown && !_triggersDown.Contains(chainConfig.trigger))
                {
                    // Rising edge: key just pressed — fire the chain once
                    _triggersDown.Add(chainConfig.trigger);
                    anyTriggered = true;
                    ExecuteMacroChain(roClient, chainConfig);
                }
                else if (isDown && _triggersDown.Contains(chainConfig.trigger) && chainConfig.infinityLoop)
                {
                    // Key still held and infinity loop enabled — repeat the chain
                    anyTriggered = true;
                    ExecuteMacroChain(roClient, chainConfig);
                }
                else if (!isDown)
                {
                    // Key released — reset so next press fires again
                    _triggersDown.Remove(chainConfig.trigger);
                }
            }

            if (!anyTriggered)
            {
                Thread.Sleep(10);
            }

            return 0;
        }

        private void ExecuteMacroChain(Client roClient, ChainConfig chainConfig)
        {
            if (InputAutomationStopProtocol.ShouldYieldBuffStyleInput())
            {
                return;
            }

            InputAutomationStopProtocol.EnterExclusiveAutomation();
            try
            {
                var keys = GetOrderedMacroEntries(chainConfig).ToList();
                if (keys.Count == 0) return;

                if (chainConfig.currentStep >= keys.Count)
                {
                    chainConfig.currentStep = 0;
                }

                MacroKey macroKey = keys[chainConfig.currentStep];
                
                if (macroKey != null && macroKey.key != Key.None)
                {
                    if (chainConfig.instrumentKey != Key.None)
                    {
                        Keys instrumentKey = (Keys)Enum.Parse(typeof(Keys), chainConfig.instrumentKey.ToString());
                        roClient.input.SendKey((int)instrumentKey, true);
                    }

                    Keys thisk = (Keys)Enum.Parse(typeof(Keys), macroKey.key.ToString());
                    Thread.Sleep(macroKey.delay);
                    roClient.input.SendKey((int)thisk, true);

                    if (macroKey.hasClick)
                    {
                        Native.POINT p;
                        Native.GetCursorPos(out p);
                        IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                        if (hWnd != IntPtr.Zero) Native.ScreenToClient(hWnd, ref p);
                        roClient.input.SendClick(p.X, p.Y);
                    }

                    if (chainConfig.daggerKey != Key.None)
                    {
                        Keys daggerKey = (Keys)Enum.Parse(typeof(Keys), chainConfig.daggerKey.ToString());
                        roClient.input.SendKey((int)daggerKey, true);
                    }
                }

                chainConfig.currentStep++;
            }
            finally
            {
                InputAutomationStopProtocol.LeaveExclusiveAutomation();
            }
        }

        private static IEnumerable<MacroKey> GetOrderedMacroEntries(ChainConfig chainConfig)
        {
            if (chainConfig?.macroEntries == null)
            {
                yield break;
            }

            foreach (KeyValuePair<string, MacroKey> entry in chainConfig.macroEntries.OrderBy(kvp => ExtractMacroSlotIndex(kvp.Key)))
            {
                yield return entry.Value;
            }
        }

        private static int ExtractMacroSlotIndex(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return int.MaxValue;
            }

            int start = key.StartsWith("in", StringComparison.OrdinalIgnoreCase) ? 2 : 0;
            int end = key.IndexOf("mac", StringComparison.OrdinalIgnoreCase);
            if (end > start && int.TryParse(key.Substring(start, end - start), out int index))
            {
                return index;
            }

            return int.MaxValue;
        }

        public void Start()
        {
            Stop();
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                this.thread = new _4RThread((_) => MacroExecutionThread(roClient));
                _4RThread.Start(this.thread);
            }
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            _triggersDown.Clear();
        }
    }
}
