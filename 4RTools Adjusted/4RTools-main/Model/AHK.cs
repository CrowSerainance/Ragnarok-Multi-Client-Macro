using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Utils;
using _4RTools.Utils.MuhBotCore;
using Newtonsoft.Json;
using FormsKeys = System.Windows.Forms.Keys;
using WpfKey = System.Windows.Input.Key;

namespace _4RTools.Model
{
    /// <summary>Legacy config kept only for profile migration from old checkbox-based AHK.</summary>
    public class KeyConfig
    {
        public WpfKey key { get; set; }
        public bool ClickActive { get; set; }

        public KeyConfig()
        {
            this.key = WpfKey.None;
        }

        public KeyConfig(WpfKey key, bool clickAtive)
        {
            this.key = key;
            this.ClickActive = clickAtive;
        }
    }

    /// <summary>One physical shortcut (used for the single slot trigger; serialized as one entry in <see cref="AhkSlotConfig.TriggerBindings"/>).</summary>
    public class AhkTriggerBinding
    {
        public string TriggerKey { get; set; } = FormsKeys.None.ToString();
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }

        public FormsKeys ParseKey() => AhkSlotConfig.ParseKeyStringStatic(this.TriggerKey);
    }

    public class AhkSlotConfig
    {
        public string SlotId { get; set; } = string.Empty;

        /// <summary>Single trigger main key (with <see cref="Ctrl"/> / <see cref="Alt"/> / <see cref="Shift"/> / <see cref="Win"/>).</summary>
        public string TriggerKey { get; set; } = FormsKeys.None.ToString();
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }

        /// <summary>At most one entry kept in sync with root trigger fields (JSON compatibility).</summary>
        public List<AhkTriggerBinding> TriggerBindings { get; set; } = new List<AhkTriggerBinding>();

        /// <summary>First skill key for legacy readers; kept in sync with <see cref="SkillKeys"/>[0].</summary>
        public string SkillKey { get; set; } = FormsKeys.None.ToString();

        /// <summary>Keys sent to the game while the trigger is held (cycled each spam tick, like vanilla AhkEntryOrder).</summary>
        public List<string> SkillKeys { get; set; } = new List<string>();

        public bool ClickActive { get; set; } = true;
        public bool Enabled { get; set; }

        [JsonIgnore]
        public bool HasBinding =>
            Enabled &&
            ParseTriggerKey() != FormsKeys.None &&
            GetResolvedSkillKeyCodes().Count > 0;

        [JsonIgnore]
        public bool HasAnyConfiguration =>
            Enabled ||
            GetResolvedSkillKeyCodes().Count > 0 ||
            ParseSkillKey() != FormsKeys.None ||
            (SkillKeys != null && SkillKeys.Count > 0) ||
            ParseTriggerKey() != FormsKeys.None ||
            Ctrl ||
            Alt ||
            Shift ||
            Win;

        /// <summary>Migrates older profiles (including reversed dialog saves) and normalizes lists.</summary>
        public void EnsureSlotModelConsistent()
        {
            if (this.TriggerBindings == null)
            {
                this.TriggerBindings = new List<AhkTriggerBinding>();
            }

            if (this.SkillKeys == null)
            {
                this.SkillKeys = new List<string>();
            }

            // Reversed dialog era: multiple TriggerBindings + single SkillKey → one trigger + many skills.
            if (this.TriggerBindings.Count > 1 && ParseSkillKey() != FormsKeys.None)
            {
                this.SkillKeys.Clear();
                foreach (AhkTriggerBinding b in this.TriggerBindings)
                {
                    if (ParseKeyStringStatic(b.TriggerKey) != FormsKeys.None)
                    {
                        this.SkillKeys.Add(b.TriggerKey);
                    }
                }

                this.TriggerKey = this.SkillKey;
                this.Ctrl = false;
                this.Alt = false;
                this.Shift = false;
                this.Win = false;
                this.TriggerBindings.Clear();
            }
            else if (this.TriggerBindings.Count > 1)
            {
                AhkTriggerBinding first = this.TriggerBindings[0];
                this.TriggerKey = first.TriggerKey;
                this.Ctrl = first.Ctrl;
                this.Alt = first.Alt;
                this.Shift = first.Shift;
                this.Win = first.Win;
                this.SkillKeys.Clear();
                for (int i = 1; i < this.TriggerBindings.Count; i++)
                {
                    AhkTriggerBinding b = this.TriggerBindings[i];
                    if (ParseKeyStringStatic(b.TriggerKey) != FormsKeys.None)
                    {
                        this.SkillKeys.Add(b.TriggerKey);
                    }
                }

                this.TriggerBindings.Clear();
            }

            if (this.TriggerBindings.Count == 0 && ParseTriggerKey() != FormsKeys.None)
            {
                this.TriggerBindings.Add(new AhkTriggerBinding
                {
                    TriggerKey = this.TriggerKey,
                    Ctrl = this.Ctrl,
                    Alt = this.Alt,
                    Shift = this.Shift,
                    Win = this.Win
                });
            }
            else if (this.TriggerBindings.Count == 1)
            {
                this.SyncLegacyFieldsFromFirstBinding();
            }

            if (this.SkillKeys.Count == 0 && ParseSkillKey() != FormsKeys.None)
            {
                this.SkillKeys.Add(this.SkillKey);
            }

            this.SyncSkillKeyFromList();
        }

        public void SyncSkillKeyFromList()
        {
            if (this.SkillKeys != null && this.SkillKeys.Count > 0)
            {
                this.SkillKey = this.SkillKeys[0];
            }
            else
            {
                this.SkillKey = FormsKeys.None.ToString();
            }
        }

        /// <summary>Keep root TriggerKey/Ctrl/... in sync with first binding for older profile readers.</summary>
        public void SyncLegacyFieldsFromFirstBinding()
        {
            if (this.TriggerBindings == null || this.TriggerBindings.Count == 0)
            {
                this.TriggerKey = FormsKeys.None.ToString();
                this.Ctrl = this.Alt = this.Shift = this.Win = false;
                return;
            }

            AhkTriggerBinding first = this.TriggerBindings[0];
            this.TriggerKey = first.TriggerKey;
            this.Ctrl = first.Ctrl;
            this.Alt = first.Alt;
            this.Shift = first.Shift;
            this.Win = first.Win;
        }

        public List<FormsKeys> GetResolvedSkillKeyCodes()
        {
            List<FormsKeys> result = new List<FormsKeys>();
            if (this.SkillKeys != null)
            {
                foreach (string s in this.SkillKeys)
                {
                    FormsKeys k = ParseKeyStringStatic(s);
                    if (k != FormsKeys.None)
                    {
                        result.Add(k);
                    }
                }
            }

            if (result.Count == 0)
            {
                FormsKeys legacy = ParseSkillKey();
                if (legacy != FormsKeys.None)
                {
                    result.Add(legacy);
                }
            }

            return result;
        }

        public FormsKeys ParseTriggerKey()
        {
            return ParseKeyStringStatic(this.TriggerKey);
        }

        public FormsKeys ParseSkillKey()
        {
            return ParseKeyStringStatic(this.SkillKey);
        }

        internal static FormsKeys ParseKeyStringStatic(string keyStr)
        {
            if (string.IsNullOrWhiteSpace(keyStr))
            {
                return FormsKeys.None;
            }

            if (Enum.TryParse(keyStr, true, out FormsKeys parsedKey))
            {
                return parsedKey;
            }

            if (keyStr.Length == 1 && char.IsDigit(keyStr[0]))
            {
                return (FormsKeys)Enum.Parse(typeof(FormsKeys), $"D{keyStr}");
            }

            return FormsKeys.None;
        }
    }

    public class AHK : Action
    {
        private const string ACTION_NAME = "AHK20";
        public const string COMPATIBILITY = "ahkCompatibility";
        public const string SPEED_BOOST = "ahkSpeedBoost";
        public const int SLOT_COUNT = 10;

        private _4RThread thread;

        // Legacy checkbox mappings kept for backward-compatible profile migration.
        public Dictionary<string, KeyConfig> AhkEntries { get; set; } = new Dictionary<string, KeyConfig>();
        public List<AhkSlotConfig> Slots { get; set; } = CreateDefaultSlots();
        public int AhkDelay { get; set; } = 10;
        public bool mouseFlick { get; set; } = false;
        public bool noShift { get; set; } = false;
        public string ahkMode { get; set; } = COMPATIBILITY;

        [JsonIgnore]
        private readonly int[] _slotSkillRotateIndex = new int[SLOT_COUNT];

        /// <summary>Last fire time from <see cref="Stopwatch.GetTimestamp"/>; 0 = trigger not held since last reset.</summary>
        [JsonIgnore]
        private readonly long[] _slotLastSkillFireTimestamp = new long[SLOT_COUNT];

        private static readonly double StopwatchTicksToMs = 1000.0 / Stopwatch.Frequency;

        public AHK()
        {
            EnsureSlotsConfigured();
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            EnsureSlotsConfigured();
        }

        public static List<AhkSlotConfig> CreateDefaultSlots()
        {
            List<AhkSlotConfig> slots = new List<AhkSlotConfig>();
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                slots.Add(new AhkSlotConfig
                {
                    SlotId = GetSlotId(i),
                    TriggerKey = FormsKeys.None.ToString(),
                    TriggerBindings = new List<AhkTriggerBinding>(),
                    SkillKeys = new List<string>(),
                    ClickActive = true,
                    Enabled = false
                });
            }

            return slots;
        }

        public void EnsureSlotsConfigured()
        {
            if (this.Slots == null)
            {
                this.Slots = new List<AhkSlotConfig>();
            }

            if (this.Slots.Count > SLOT_COUNT)
            {
                this.Slots = this.Slots.Take(SLOT_COUNT).ToList();
            }

            while (this.Slots.Count < SLOT_COUNT)
            {
                this.Slots.Add(new AhkSlotConfig
                {
                    SlotId = GetSlotId(this.Slots.Count),
                    TriggerKey = FormsKeys.None.ToString(),
                    TriggerBindings = new List<AhkTriggerBinding>(),
                    SkillKeys = new List<string>(),
                    ClickActive = true,
                    Enabled = false
                });
            }

            for (int i = 0; i < this.Slots.Count; i++)
            {
                if (this.Slots[i] == null)
                {
                    this.Slots[i] = new AhkSlotConfig();
                }

                if (string.IsNullOrWhiteSpace(this.Slots[i].SlotId))
                {
                    this.Slots[i].SlotId = GetSlotId(i);
                }

                if (string.IsNullOrWhiteSpace(this.Slots[i].TriggerKey))
                {
                    this.Slots[i].TriggerKey = FormsKeys.None.ToString();
                }

                if (string.IsNullOrWhiteSpace(this.Slots[i].SkillKey))
                {
                    this.Slots[i].SkillKey = FormsKeys.None.ToString();
                }

                if (this.Slots[i].TriggerBindings == null)
                {
                    this.Slots[i].TriggerBindings = new List<AhkTriggerBinding>();
                }

                if (this.Slots[i].SkillKeys == null)
                {
                    this.Slots[i].SkillKeys = new List<string>();
                }

                this.Slots[i].EnsureSlotModelConsistent();
            }

            if (this.AhkDelay < 0)
            {
                this.AhkDelay = 0;
            }

            if (string.IsNullOrWhiteSpace(this.ahkMode) ||
                (!string.Equals(this.ahkMode, COMPATIBILITY, StringComparison.Ordinal) &&
                 !string.Equals(this.ahkMode, SPEED_BOOST, StringComparison.Ordinal)))
            {
                this.ahkMode = COMPATIBILITY;
            }

            MigrateLegacyEntriesIfNeeded();
        }

        public void SetSlot(int index, AhkSlotConfig slot)
        {
            EnsureSlotsConfigured();
            if (index < 0 || index >= this.Slots.Count || slot == null)
            {
                return;
            }

            slot.SlotId = GetSlotId(index);
            if (string.IsNullOrWhiteSpace(slot.TriggerKey))
            {
                slot.TriggerKey = FormsKeys.None.ToString();
            }

            if (string.IsNullOrWhiteSpace(slot.SkillKey))
            {
                slot.SkillKey = FormsKeys.None.ToString();
            }

            if (slot.TriggerBindings == null)
            {
                slot.TriggerBindings = new List<AhkTriggerBinding>();
            }

            if (slot.SkillKeys == null)
            {
                slot.SkillKeys = new List<string>();
            }

            slot.EnsureSlotModelConsistent();
            this.Slots[index] = slot;
        }

        public void Start()
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient != null)
            {
                EnsureSlotsConfigured();

                if (thread != null)
                {
                    _4RThread.Stop(this.thread);
                }

                this.thread = new _4RThread(_ => AHKThreadExecution(roClient));
                _4RThread.Start(this.thread);
            }
        }

        private int AHKThreadExecution(Client roClient)
        {
            EnsureSlotsConfigured();

            bool skillSpammerLaneActive = false;
            for (int s = 0; s < this.Slots.Count; s++)
            {
                AhkSlotConfig probe = this.Slots[s];
                if (probe.HasBinding && IsSlotPressed(probe))
                {
                    skillSpammerLaneActive = true;
                    break;
                }
            }

            if (skillSpammerLaneActive)
            {
                InputAutomationStopProtocol.EnterExclusiveAutomation();
            }

            try
            {
                int delayMs = this.AhkDelay;

                for (int i = 0; i < this.Slots.Count; i++)
                {
                    AhkSlotConfig slot = this.Slots[i];
                    if (!IsSlotPressed(slot))
                    {
                        this._slotSkillRotateIndex[i] = 0;
                        this._slotLastSkillFireTimestamp[i] = 0;
                        continue;
                    }

                    if (!slot.HasBinding)
                    {
                        continue;
                    }

                    long lastTs = this._slotLastSkillFireTimestamp[i];
                    long tsNow = Stopwatch.GetTimestamp();
                    if (lastTs != 0 && delayMs > 0)
                    {
                        double elapsedMs = (tsNow - lastTs) * StopwatchTicksToMs;
                        if (elapsedMs < delayMs)
                        {
                            continue;
                        }
                    }

                    this._slotLastSkillFireTimestamp[i] = tsNow;
                    this.FireSlotOnce(roClient, slot, i);
                }
            }
            finally
            {
                if (skillSpammerLaneActive)
                {
                    InputAutomationStopProtocol.LeaveExclusiveAutomation();
                }
            }

            return 0;
        }

        /// <summary>
        /// One send per throttled tick. Uses <see cref="InputSimulator.SendKey"/> so F-keys and row keys get a real
        /// down/hold/up cadence; raw PostMessage down+5ms+up was dropping inputs. Click mode: tap skill then click
        /// (RO expects a key press then ground click, not a held key through the whole mouse sequence).
        /// </summary>
        private void FireSlotOnce(Client roClient, AhkSlotConfig slot, int slotIndex)
        {
            if (roClient?.processManager == null ||
                roClient.processManager.GetGameWindowOrNull() == IntPtr.Zero)
            {
                return;
            }

            List<FormsKeys> skillCodes = slot.GetResolvedSkillKeyCodes();
            if (skillCodes.Count == 0)
            {
                return;
            }

            int count = skillCodes.Count;
            int rot = this._slotSkillRotateIndex[slotIndex];
            int idx = (int)((uint)rot % (uint)count);
            this._slotSkillRotateIndex[slotIndex] = rot + 1;
            int skillVk = (int)skillCodes[idx];

            bool useSpeedBoost = string.Equals(this.ahkMode, SPEED_BOOST, StringComparison.Ordinal);
            bool shouldHoldShift = this.noShift && !slot.Shift && !useSpeedBoost;
            bool shouldClick = slot.ClickActive || useSpeedBoost;
            bool useFlick = this.mouseFlick && !useSpeedBoost;

            if (shouldHoldShift)
            {
                roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
            }

            try
            {
                roClient.input.SendKey(skillVk, true);

                if (shouldClick)
                {
                    Native.POINT p;
                    Native.GetCursorPos(out p);
                    IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                    if (hWnd != IntPtr.Zero)
                    {
                        Native.ScreenToClient(hWnd, ref p);
                    }

                    if (useFlick)
                    {
                        roClient.input.PostClickWithFlick(p.X, p.Y);
                    }
                    else
                    {
                        roClient.input.PostClick(p.X, p.Y);
                    }
                }
            }
            finally
            {
                if (shouldHoldShift)
                {
                    roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
                }
            }
        }

        private static bool IsSlotPressed(AhkSlotConfig slot)
        {
            var binding = new AhkTriggerBinding
            {
                TriggerKey = slot.TriggerKey,
                Ctrl = slot.Ctrl,
                Alt = slot.Alt,
                Shift = slot.Shift,
                Win = slot.Win
            };

            return IsTriggerBindingPressed(binding);
        }

        private static bool IsTriggerBindingPressed(AhkTriggerBinding binding)
        {
            FormsKeys keyCode = binding.ParseKey();
            if (keyCode == FormsKeys.None)
            {
                return false;
            }

            return IsKeyDown(keyCode) &&
                   IsModifierStateEqual(binding.Ctrl, FormsKeys.LControlKey, FormsKeys.RControlKey) &&
                   IsModifierStateEqual(binding.Alt, FormsKeys.LMenu, FormsKeys.RMenu) &&
                   IsModifierStateEqual(binding.Shift, FormsKeys.LShiftKey, FormsKeys.RShiftKey) &&
                   IsModifierStateEqual(binding.Win, FormsKeys.LWin, FormsKeys.RWin);
        }

        private static bool IsModifierStateEqual(bool expectedDown, FormsKeys leftKey, FormsKeys rightKey)
        {
            bool isDown = IsKeyDown(leftKey) || IsKeyDown(rightKey);
            return expectedDown == isDown;
        }

        private static bool IsKeyDown(FormsKeys key)
        {
            return (Native.GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private void MigrateLegacyEntriesIfNeeded()
        {
            if (this.AhkEntries == null || this.AhkEntries.Count == 0)
            {
                return;
            }

            bool hasConfiguredSlots = this.Slots.Any(slot => slot.HasAnyConfiguration);
            if (hasConfiguredSlots)
            {
                return;
            }

            int slotIndex = 0;
            foreach (KeyValuePair<string, KeyConfig> entry in this.AhkEntries.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (slotIndex >= SLOT_COUNT)
                {
                    break;
                }

                KeyConfig legacyConfig = entry.Value;
                if (legacyConfig == null)
                {
                    continue;
                }

                string normalizedKey = NormalizeLegacyKeyName(legacyConfig.key.ToString());
                this.Slots[slotIndex] = new AhkSlotConfig
                {
                    SlotId = GetSlotId(slotIndex),
                    TriggerKey = normalizedKey,
                    SkillKey = normalizedKey,
                    SkillKeys = new List<string> { normalizedKey },
                    TriggerBindings = new List<AhkTriggerBinding>
                    {
                        new AhkTriggerBinding { TriggerKey = normalizedKey }
                    },
                    ClickActive = legacyConfig.ClickActive,
                    Enabled = legacyConfig.key != WpfKey.None
                };

                slotIndex++;
            }

            this.AhkEntries.Clear();
        }

        private static string NormalizeLegacyKeyName(string legacyKeyName)
        {
            if (string.IsNullOrWhiteSpace(legacyKeyName))
            {
                return FormsKeys.None.ToString();
            }

            if (legacyKeyName.Length == 2 && legacyKeyName[0] == 'D' && char.IsDigit(legacyKeyName[1]))
            {
                return legacyKeyName;
            }

            return legacyKeyName;
        }

        private static string GetSlotId(int index)
        {
            return $"slot{index + 1:D2}";
        }

        public void AddAHKEntry(string chkboxName, KeyConfig value)
        {
            if (this.AhkEntries.ContainsKey(chkboxName))
            {
                RemoveAHKEntry(chkboxName);
            }

            this.AhkEntries.Add(chkboxName, value);
        }

        public void RemoveAHKEntry(string chkboxName)
        {
            this.AhkEntries.Remove(chkboxName);
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
        }

        public string GetConfiguration()
        {
            EnsureSlotsConfigured();
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return ACTION_NAME;
        }
    }
}
