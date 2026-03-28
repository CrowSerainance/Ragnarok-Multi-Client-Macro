using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>One physical shortcut (used for the single slot trigger).</summary>
    public class AhkTriggerBinding
    {
        public string TriggerKey { get; set; } = FormsKeys.None.ToString();
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }

        public FormsKeys ParseKey() => AhkSlotConfig.ParseKeyStringStatic(this.TriggerKey);
    }

    /// <summary>One RO skill bar hotkey (physical key + optional Ctrl/Alt/Shift), not a skill ID.</summary>
    public class AhkSkillBinding
    {
        public string Key { get; set; } = FormsKeys.None.ToString();
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }

        public FormsKeys ParseKey() => AhkSlotConfig.ParseKeyStringStatic(this.Key);

        public string BuildSignature()
        {
            FormsKeys keyCode = this.ParseKey();
            if (keyCode == FormsKeys.None)
            {
                return string.Empty;
            }

            return AhkSlotConfig.BuildSkillBindingSignature(keyCode, this.Ctrl, this.Alt, this.Shift);
        }
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

        /// <summary>First hotkey for legacy JSON readers; kept in sync with <see cref="SkillKeys"/>[0].</summary>
        public string SkillKey { get; set; } = FormsKeys.None.ToString();

        /// <summary>Plain key names for backward compat; kept in sync with <see cref="SkillBindings"/>.</summary>
        public List<string> SkillKeys { get; set; } = new List<string>();

        /// <summary>Skill bar hotkeys with optional modifiers. JSON property name unchanged for profile compatibility.</summary>
        public List<AhkSkillBinding> SkillBindings { get; set; } = new List<AhkSkillBinding>();

        public bool Enabled { get; set; }

        /// <summary>Pause between hotkeys in a chain (after each key ± click, before the next key). Not applied after the last key (ms; default 60, range 20–350).</summary>
        public int InterSkillDelayMs { get; set; } = 60;

        /// <summary>Click at cursor after each skill key to confirm targeting. Default on.</summary>
        public bool ClickActive { get; set; } = true;

        /// <summary>Cycles through skill keys one-per-press so the game's aftercast delay is respected.</summary>
        [JsonIgnore]
        public int currentStep { get; set; } = 0;

        [JsonIgnore]
        public bool HasBinding =>
            Enabled &&
            ParseTriggerKey() != FormsKeys.None &&
            GetResolvedSkillBindings().Count > 0;

        [JsonIgnore]
        public bool HasAnyConfiguration =>
            Enabled ||
            GetResolvedSkillBindings().Count > 0 ||
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

            if (this.SkillBindings == null)
            {
                this.SkillBindings = new List<AhkSkillBinding>();
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

            // Migrate plain SkillKeys → SkillBindings (no modifiers) if SkillBindings is empty.
            if (this.SkillBindings.Count == 0 && this.SkillKeys.Count > 0)
            {
                foreach (string s in this.SkillKeys)
                {
                    FormsKeys k = ParseKeyStringStatic(s);
                    if (k != FormsKeys.None)
                    {
                        this.SkillBindings.Add(new AhkSkillBinding { Key = s });
                    }
                }
            }

            // Repair: SkillKeys longer than SkillBindings (partial saves / JSON desync → only first skill ran).
            if (this.SkillKeys.Count > this.SkillBindings.Count)
            {
                List<AhkSkillBinding> rebuilt = new List<AhkSkillBinding>();
                foreach (string s in this.SkillKeys)
                {
                    if (ParseKeyStringStatic(s) != FormsKeys.None)
                    {
                        rebuilt.Add(new AhkSkillBinding { Key = s });
                    }
                }

                if (rebuilt.Count > this.SkillBindings.Count)
                {
                    this.SkillBindings = rebuilt;
                }
            }

            // Fallback: single legacy SkillKey
            if (this.SkillBindings.Count == 0 && this.SkillKeys.Count == 0 && ParseSkillKey() != FormsKeys.None)
            {
                this.SkillBindings.Add(new AhkSkillBinding { Key = this.SkillKey });
                this.SkillKeys.Add(this.SkillKey);
            }

            // Keep SkillKeys in sync from SkillBindings for backward compat.
            if (this.SkillBindings.Count > 0)
            {
                this.SkillBindings = DeduplicateSkillBindings(this.SkillBindings);
                this.SkillKeys = this.SkillBindings
                    .Where(b => b.ParseKey() != FormsKeys.None)
                    .Select(b => b.Key)
                    .ToList();
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

        /// <summary>Returns resolved skill bar hotkeys (modifiers included). Primary method for fire logic.</summary>
        public List<AhkSkillBinding> GetResolvedSkillBindings()
        {
            if (this.SkillBindings != null && this.SkillBindings.Count > 0)
            {
                return DeduplicateSkillBindings(this.SkillBindings);
            }

            // Fallback to plain SkillKeys (no modifiers).
            List<AhkSkillBinding> result = new List<AhkSkillBinding>();
            if (this.SkillKeys != null)
            {
                foreach (string s in this.SkillKeys)
                {
                    FormsKeys k = ParseKeyStringStatic(s);
                    if (k != FormsKeys.None)
                    {
                        result.Add(new AhkSkillBinding { Key = s });
                    }
                }
            }

            if (result.Count == 0)
            {
                FormsKeys legacy = ParseSkillKey();
                if (legacy != FormsKeys.None)
                {
                    result.Add(new AhkSkillBinding { Key = legacy.ToString() });
                }
            }

            return DeduplicateSkillBindings(result);
        }

        /// <summary>Convenience: just the key codes (no modifier info). Used by UI validation.</summary>
        public List<FormsKeys> GetResolvedSkillKeyCodes()
        {
            return GetResolvedSkillBindings()
                .Select(b => b.ParseKey())
                .Where(k => k != FormsKeys.None)
                .ToList();
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

            string normalized = NormalizeKeyAlias(keyStr.Trim());
            if (Enum.TryParse(normalized, true, out FormsKeys parsedKey))
            {
                return parsedKey;
            }

            if (normalized.Length == 1 && char.IsDigit(normalized[0]))
            {
                return (FormsKeys)Enum.Parse(typeof(FormsKeys), $"D{normalized}");
            }

            // JSON/tools sometimes store virtual-key as decimal ("113" = F2) or hex ("0x71").
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(normalized.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int vkHex) &&
                vkHex >= 1 && vkHex <= 255)
            {
                return (FormsKeys)vkHex;
            }

            if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out int vkDec) &&
                vkDec >= 1 && vkDec <= 255)
            {
                return (FormsKeys)vkDec;
            }

            return FormsKeys.None;
        }

        private static string NormalizeKeyAlias(string keyStr)
        {
            string trimmed = keyStr.Trim();
            switch (trimmed)
            {
                case "PageUp":
                    return FormsKeys.Prior.ToString();
                case "PageDown":
                    return FormsKeys.Next.ToString();
                case "Enter":
                    return FormsKeys.Return.ToString();
                case "-":
                    return FormsKeys.OemMinus.ToString();
                case "=":
                    return FormsKeys.Oemplus.ToString();
                case "[":
                    return FormsKeys.OemOpenBrackets.ToString();
                case "]":
                    return FormsKeys.OemCloseBrackets.ToString();
                case "\\":
                    return FormsKeys.OemPipe.ToString();
                case ";":
                    return FormsKeys.OemSemicolon.ToString();
                case "`":
                    return FormsKeys.Oemtilde.ToString();
                case "'":
                    return FormsKeys.OemQuotes.ToString();
                case ",":
                    return FormsKeys.Oemcomma.ToString();
                case ".":
                    return FormsKeys.OemPeriod.ToString();
                case "/":
                    return FormsKeys.OemQuestion.ToString();
                default:
                    return trimmed;
            }
        }

        internal static string BuildSkillBindingSignature(AhkSkillBinding binding)
        {
            if (binding == null)
            {
                return string.Empty;
            }

            return BuildSkillBindingSignature(binding.ParseKey(), binding.Ctrl, binding.Alt, binding.Shift);
        }

        internal static string BuildSkillBindingSignature(FormsKeys keyCode, bool ctrl, bool alt, bool shift)
        {
            if (keyCode == FormsKeys.None)
            {
                return string.Empty;
            }

            return $"{(int)keyCode}|{ctrl}|{alt}|{shift}";
        }

        private static List<AhkSkillBinding> DeduplicateSkillBindings(IEnumerable<AhkSkillBinding> bindings)
        {
            List<AhkSkillBinding> result = new List<AhkSkillBinding>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            if (bindings == null)
            {
                return result;
            }

            foreach (AhkSkillBinding binding in bindings)
            {
                if (binding == null)
                {
                    continue;
                }

                FormsKeys keyCode = binding.ParseKey();
                if (keyCode == FormsKeys.None)
                {
                    continue;
                }

                string signature = BuildSkillBindingSignature(keyCode, binding.Ctrl, binding.Alt, binding.Shift);
                if (!seen.Add(signature))
                {
                    continue;
                }

                result.Add(new AhkSkillBinding
                {
                    Key = keyCode.ToString(),
                    Ctrl = binding.Ctrl,
                    Alt = binding.Alt,
                    Shift = binding.Shift
                });
            }

            return result;
        }
    }

    public class AHK : Action
    {
        private const string ACTION_NAME = "AHK20";
        public const int SLOT_COUNT = 10;

        /// <summary>Minimum pause between keys in a chain (ms). Property name unchanged for JSON.</summary>
        public const int InterSkillDelayMinMs = 20;

        /// <summary>Maximum pause between keys in bind UI and chain execution (ms).</summary>
        public const int InterSkillDelayMaxMs = 500;

        private _4RThread thread;

        /// <summary>Rising-edge detection: tracks which slot indices have their trigger held.</summary>
        [JsonIgnore]
        private readonly HashSet<int> _slotTriggerDown = new HashSet<int>();

        /// <summary>0 = idle, 1 = worker running (Interlocked; avoids torn reads between poll thread and threadpool).</summary>
        [JsonIgnore]
        private readonly int[] _slotBusy = new int[SLOT_COUNT];

        /// <summary>Set when AHK is stopped so in-flight runs bail out.</summary>
        [JsonIgnore]
        private volatile bool _stopped;

        // Legacy checkbox mappings kept for backward-compatible profile migration.
        public Dictionary<string, KeyConfig> AhkEntries { get; set; } = new Dictionary<string, KeyConfig>();
        public List<AhkSlotConfig> Slots { get; set; } = CreateDefaultSlots();

        /// <summary>Classic Skill Spammer modes (same names as stock 4RTools).</summary>
        public const string COMPATIBILITY = "ahkCompatibility";
        public const string SPEED_BOOST = "ahkSpeedBoost";

        /// <summary>Legacy delay field (stock profiles); per-slot chains use <see cref="AhkSlotConfig.InterSkillDelayMs"/>.</summary>
        public int AhkDelay { get; set; } = 10;

        public bool mouseFlick { get; set; }
        public bool noShift { get; set; }
        public string ahkMode { get; set; } = COMPATIBILITY;

        public AHK()
        {
            EnsureSlotsConfigured();
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (string.IsNullOrWhiteSpace(this.ahkMode))
            {
                this.ahkMode = COMPATIBILITY;
            }

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
                    SkillBindings = new List<AhkSkillBinding>(),
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
                    SkillBindings = new List<AhkSkillBinding>(),
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

                if (this.Slots[i].SkillBindings == null)
                {
                    this.Slots[i].SkillBindings = new List<AhkSkillBinding>();
                }

                this.Slots[i].EnsureSlotModelConsistent();
                this.Slots[i].InterSkillDelayMs = Math.Max(
                    InterSkillDelayMinMs,
                    Math.Min(this.Slots[i].InterSkillDelayMs, InterSkillDelayMaxMs));
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

            if (slot.SkillBindings == null)
            {
                slot.SkillBindings = new List<AhkSkillBinding>();
            }

            slot.EnsureSlotModelConsistent();
            slot.InterSkillDelayMs = Math.Max(
                InterSkillDelayMinMs,
                Math.Min(slot.InterSkillDelayMs, InterSkillDelayMaxMs));

            this.Slots[index] = slot;
        }

        public void Start()
        {
            EnsureSlotsConfigured();

            // Signal in-flight workers to bail out of sleeps / chain loops.
            this._stopped = true;
            try
            {
                if (this.thread != null)
                {
                    _4RThread.Stop(this.thread);
                }

                // Stale Task.Run workers can outlive the poll thread; wait before resetting state.
                WaitForSlotWorkersIdle(800);

                this._slotTriggerDown.Clear();
                for (int i = 0; i < SLOT_COUNT; i++)
                {
                    Interlocked.Exchange(ref this._slotBusy[i], 0);
                }
            }
            finally
            {
                this._stopped = false;
            }

            this.thread = new _4RThread(_ => AHKThreadExecution());
            _4RThread.Start(this.thread);
        }

        /// <summary>Waits until no slot worker holds the busy flag (poll thread is already stopped).</summary>
        private void WaitForSlotWorkersIdle(int timeoutMs)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                bool anyBusy = false;
                for (int i = 0; i < SLOT_COUNT; i++)
                {
                    if (Volatile.Read(ref this._slotBusy[i]) != 0)
                    {
                        anyBusy = true;
                        break;
                    }
                }

                if (!anyBusy)
                {
                    return;
                }

                Thread.Sleep(5);
            }
        }

        /// <summary>Sleep in small chunks so <see cref="_stopped"/> can abort waits promptly.</summary>
        private void SleepWhileRunning(int totalMs)
        {
            int remaining = totalMs;
            while (remaining > 0 && !this._stopped)
            {
                int chunk = Math.Min(15, remaining);
                Thread.Sleep(chunk);
                remaining -= chunk;
            }
        }

        /// <summary>
        /// Poll callback (every 5ms). Rising edge → run the full registered hotkey chain once (with pauses).
        /// Holding the trigger does not restart the chain; release and press again to repeat.
        /// Duplicate trigger signatures: only the first slot in index order runs; others still track held state so release clears.
        /// </summary>
        private int AHKThreadExecution()
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null)
            {
                return 0;
            }

            HashSet<string> handledTriggerSignatures = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < this.Slots.Count; i++)
            {
                // Stock 4RTools AHK ignores skill spam while Alt is held (UI / keybind safety).
                if (IsKeyDown(FormsKeys.LMenu) || IsKeyDown(FormsKeys.RMenu))
                {
                    continue;
                }

                AhkSlotConfig slot = this.Slots[i];
                if (slot == null)
                {
                    if (this._slotTriggerDown.Contains(i))
                    {
                        this._slotTriggerDown.Remove(i);
                    }

                    continue;
                }

                bool isPressed = slot.HasBinding && IsSlotPressed(slot);

                if (!isPressed && this._slotTriggerDown.Contains(i))
                {
                    this._slotTriggerDown.Remove(i);
                    continue;
                }

                if (!slot.HasBinding)
                {
                    continue;
                }

                string triggerSignature = BuildTriggerSignature(slot);
                if (isPressed && !string.IsNullOrEmpty(triggerSignature) &&
                    !handledTriggerSignatures.Add(triggerSignature))
                {
                    this._slotTriggerDown.Add(i);
                    continue;
                }

                if (isPressed && !this._slotTriggerDown.Contains(i))
                {
                    if (Interlocked.CompareExchange(ref this._slotBusy[i], 1, 0) == 0)
                    {
                        this._slotTriggerDown.Add(i);
                        int idx = i;
                        AhkSlotConfig capturedSlot = slot;
                        Client capturedClient = roClient;
                        Task.Run(() =>
                        {
                            try
                            {
                                InputAutomationStopProtocol.EnterExclusiveAutomation();
                                try
                                {
                                    FireRegisteredKeyChain(capturedClient, capturedSlot);
                                }
                                finally
                                {
                                    InputAutomationStopProtocol.LeaveExclusiveAutomation();
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[AHK] Slot worker error: {ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Exchange(ref this._slotBusy[idx], 0);
                            }
                        });
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// One trigger press → fire the NEXT single key in the cycle (step-by-step).
        /// Advances the step index so the next press fires the following key.
        /// This respects RO's aftercast delay / animation lock — the game can only
        /// process one skill action per human key-press window anyway, so blasting
        /// the entire list in 60 ms gaps just wastes every key after the first.
        /// </summary>
        private void FireRegisteredKeyChain(Client roClient, AhkSlotConfig slot)
        {
            if (this._stopped)
            {
                return;
            }

            slot.EnsureSlotModelConsistent();
            List<AhkSkillBinding> resolved = slot.GetResolvedSkillBindings();
            if (resolved.Count == 0)
            {
                return;
            }

            Client live = ClientSingleton.GetClient() ?? roClient;
            if (live?.processManager == null || live.input == null || !live.processManager.IsAttached)
            {
                return;
            }

            // Wrap around if past the end of the list
            if (slot.currentStep >= resolved.Count)
            {
                slot.currentStep = 0;
            }

            AhkSkillBinding key = resolved[slot.currentStep];
            int vk = (int)key.ParseKey();

            if (vk != (int)FormsKeys.None)
            {
                bool speedBoost = string.Equals(this.ahkMode, SPEED_BOOST, StringComparison.Ordinal);
                try
                {
                    FireVanillaSkillStep(live, slot, key, vk, speedBoost);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AHK] Slot {slot.SlotId} key send: {ex.Message}");
                }
            }

            slot.currentStep++;
        }

        /// <summary>Matches stock <c>FireOnceWithClick</c> / <c>FireOnceSpeedBoost</c> / <c>FireOnceKeyOnly</c> behavior.</summary>
        private void FireVanillaSkillStep(Client roClient, AhkSlotConfig slot, AhkSkillBinding binding, int vk, bool speedBoost)
        {
            if (this.noShift)
            {
                roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
            }

            if (binding.Ctrl || binding.Alt || binding.Shift)
            {
                roClient.input.SendKeyChord(vk, binding.Ctrl, binding.Alt, binding.Shift, false, true);
            }
            else
            {
                roClient.input.SendKey(vk, true);
            }

            if (slot.ClickActive)
            {
                CursorClientPoint(roClient, out int cx, out int cy);
                if (speedBoost)
                {
                    roClient.input.ClickAt(cx, cy);
                }
                else if (this.mouseFlick)
                {
                    roClient.input.ClickAtWithFlick(cx, cy);
                }
                else
                {
                    roClient.input.ClickAt(cx, cy);
                }
            }

            if (this.noShift)
            {
                roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
            }
        }

        private static void CursorClientPoint(Client roClient, out int cx, out int cy)
        {
            Native.POINT p;
            Native.GetCursorPos(out p);
            IntPtr hWnd = roClient.processManager.GetGameWindowOrNull();
            if (hWnd != IntPtr.Zero)
            {
                Native.ScreenToClient(hWnd, ref p);
            }

            cx = p.X;
            cy = p.Y;
        }

        private static bool IsSlotPressed(AhkSlotConfig slot)
        {
            if (slot == null)
            {
                return false;
            }

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
            if (binding == null)
            {
                return false;
            }

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
                    SkillBindings = new List<AhkSkillBinding>
                    {
                        new AhkSkillBinding { Key = normalizedKey }
                    },
                    TriggerBindings = new List<AhkTriggerBinding>
                    {
                        new AhkTriggerBinding { TriggerKey = normalizedKey }
                    },
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
            this._stopped = true;
            _4RThread.Stop(this.thread);
            this._slotTriggerDown.Clear();
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                Interlocked.Exchange(ref this._slotBusy[i], 0);
            }
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

        public bool IsMainKeyClaimedByActiveSlot(FormsKeys keyCode)
        {
            EnsureSlotsConfigured();
            if (keyCode == FormsKeys.None)
            {
                return false;
            }

            foreach (AhkSlotConfig slot in this.Slots)
            {
                if (!slot.HasBinding)
                {
                    continue;
                }

                if (slot.ParseTriggerKey() != keyCode)
                {
                    continue;
                }

                if (IsSlotPressed(slot))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTriggerClaimedByActiveSlot(FormsKeys keyCode, bool ctrl, bool alt, bool shift, bool win)
        {
            EnsureSlotsConfigured();
            string expectedSignature = BuildTriggerSignature(keyCode, ctrl, alt, shift, win);
            if (string.IsNullOrEmpty(expectedSignature))
            {
                return false;
            }

            foreach (AhkSlotConfig slot in this.Slots)
            {
                if (!slot.HasBinding)
                {
                    continue;
                }

                if (!string.Equals(BuildTriggerSignature(slot), expectedSignature, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsSlotPressed(slot))
                {
                    return true;
                }
            }

            return false;
        }

        public static string BuildTriggerSignature(AhkSlotConfig slot)
        {
            if (slot == null)
            {
                return string.Empty;
            }

            return BuildTriggerSignature(slot.ParseTriggerKey(), slot.Ctrl, slot.Alt, slot.Shift, slot.Win);
        }

        public static string BuildTriggerSignature(FormsKeys keyCode, bool ctrl, bool alt, bool shift, bool win)
        {
            if (keyCode == FormsKeys.None)
            {
                return string.Empty;
            }

            return $"{(int)keyCode}|{ctrl}|{alt}|{shift}|{win}";
        }
    }
}
