using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
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

    public class AhkSlotConfig
    {
        public string SlotId { get; set; } = string.Empty;
        public string TriggerKey { get; set; } = FormsKeys.None.ToString();
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }
        public bool ClickActive { get; set; } = true;
        public bool Enabled { get; set; }

        [JsonIgnore]
        public bool HasBinding => Enabled && ParseTriggerKey() != FormsKeys.None;

        [JsonIgnore]
        public bool HasAnyConfiguration =>
            Enabled ||
            ParseTriggerKey() != FormsKeys.None ||
            Ctrl ||
            Alt ||
            Shift ||
            Win;

        public FormsKeys ParseTriggerKey()
        {
            if (string.IsNullOrWhiteSpace(this.TriggerKey))
            {
                return FormsKeys.None;
            }

            if (Enum.TryParse(this.TriggerKey, true, out FormsKeys parsedKey))
            {
                return parsedKey;
            }

            if (this.TriggerKey.Length == 1 && char.IsDigit(this.TriggerKey[0]))
            {
                return (FormsKeys)Enum.Parse(typeof(FormsKeys), $"D{this.TriggerKey}");
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

            foreach (AhkSlotConfig slot in this.Slots)
            {
                if (!slot.HasBinding || !IsSlotPressed(slot))
                {
                    continue;
                }

                if (ahkMode.Equals(COMPATIBILITY))
                {
                    if (slot.ClickActive)
                    {
                        if (noShift && !slot.Shift)
                        {
                            roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
                        }

                        _AHKCompatibility(roClient, slot);

                        if (noShift && !slot.Shift)
                        {
                            roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
                        }
                    }
                    else
                    {
                        _AHKNoClick(roClient, slot);
                    }
                }
                else
                {
                    _AHKSpeedBoost(roClient, slot);
                }
            }

            return 0;
        }

        private void _AHKCompatibility(Client roClient, AhkSlotConfig slot)
        {
            if (this.mouseFlick)
            {
                while (IsSlotPressed(slot))
                {
                    SendSlotKey(roClient, slot);
                    Native.POINT p;
                    Native.GetCursorPos(out p);
                    IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                    if (hWnd != IntPtr.Zero) Native.ScreenToClient(hWnd, ref p);

                    roClient.input.ClickAtWithFlick(p.X, p.Y);
                    Thread.Sleep(this.AhkDelay);
                }
            }
            else
            {
                while (IsSlotPressed(slot))
                {
                    SendSlotKey(roClient, slot);
                    Native.POINT p;
                    Native.GetCursorPos(out p);
                    IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                    if (hWnd != IntPtr.Zero) Native.ScreenToClient(hWnd, ref p);
                    roClient.input.ClickAt(p.X, p.Y);
                    Thread.Sleep(this.AhkDelay);
                }
            }
        }

        private void _AHKSpeedBoost(Client roClient, AhkSlotConfig slot)
        {
            while (IsSlotPressed(slot))
            {
                SendSlotKey(roClient, slot);
                Native.POINT p;
                Native.GetCursorPos(out p);
                IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
                if (hWnd != IntPtr.Zero) Native.ScreenToClient(hWnd, ref p);
                roClient.input.ClickAt(p.X, p.Y);
                Thread.Sleep(this.AhkDelay);
            }
        }

        private void _AHKNoClick(Client roClient, AhkSlotConfig slot)
        {
            while (IsSlotPressed(slot))
            {
                SendSlotKey(roClient, slot);
                Thread.Sleep(this.AhkDelay);
            }
        }

        private void SendSlotKey(Client roClient, AhkSlotConfig slot)
        {
            FormsKeys keyCode = slot.ParseTriggerKey();
            if (keyCode == FormsKeys.None)
            {
                return;
            }

            roClient.input.SendKeyChord(
                (int)keyCode,
                slot.Ctrl,
                slot.Alt,
                slot.Shift,
                slot.Win,
                true);
        }

        private static bool IsSlotPressed(AhkSlotConfig slot)
        {
            FormsKeys keyCode = slot.ParseTriggerKey();
            if (keyCode == FormsKeys.None)
            {
                return false;
            }

            return IsKeyDown(keyCode) &&
                   IsModifierStateEqual(slot.Ctrl, FormsKeys.LControlKey, FormsKeys.RControlKey) &&
                   IsModifierStateEqual(slot.Alt, FormsKeys.LMenu, FormsKeys.RMenu) &&
                   IsModifierStateEqual(slot.Shift, FormsKeys.LShiftKey, FormsKeys.RShiftKey) &&
                   IsModifierStateEqual(slot.Win, FormsKeys.LWin, FormsKeys.RWin);
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

                this.Slots[slotIndex] = new AhkSlotConfig
                {
                    SlotId = GetSlotId(slotIndex),
                    TriggerKey = NormalizeLegacyKeyName(legacyConfig.key.ToString()),
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
