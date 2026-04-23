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
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<AhkTriggerBinding> TriggerBindings { get; set; } = new List<AhkTriggerBinding>();

        /// <summary>First hotkey for legacy JSON readers; kept in sync with <see cref="SkillKeys"/>[0].</summary>
        public string SkillKey { get; set; } = FormsKeys.None.ToString();

        /// <summary>Plain key names for backward compat; kept in sync with <see cref="SkillBindings"/>.</summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<string> SkillKeys { get; set; } = new List<string>();

        /// <summary>Skill bar hotkeys with optional modifiers. JSON property name unchanged for profile compatibility.</summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public List<AhkSkillBinding> SkillBindings { get; set; } = new List<AhkSkillBinding>();

        public bool Enabled { get; set; }

        /// <summary>Pause between hotkeys in a chain (after each key ± click, before the next key). Not applied after the last key (ms; default 60, range 20–350).</summary>
        public int InterSkillDelayMs { get; set; } = 60;

        /// <summary>
        /// Per-skill delay overrides keyed by <see cref="AhkSkillBinding.BuildSignature"/>. Looked up
        /// after each fire to pace the next loop iteration; missing/invalid entries fall back to
        /// <see cref="InterSkillDelayMs"/>. RO aftercast varies wildly per skill (Bolt vs Asura vs Heal),
        /// so a single global delay is always wrong for half of them.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, int> PerSkillDelayMs { get; set; } = new Dictionary<string, int>();

        /// <summary>Click at cursor after each skill key to confirm targeting. Default on.</summary>
        public bool ClickActive { get; set; } = true;

        /// <summary>When true, holding the trigger auto-cycles through skill keys paced by <see cref="InterSkillDelayMs"/>.</summary>
        public bool LoopMode { get; set; }

        /// <summary>
        /// Optional minimum HP% for fire (1–100). null = no lower-bound gate.
        /// Use case: don't waste keystrokes when HP is dangerously low (let a heal slot take priority).
        /// </summary>
        public int? MinHpPercent { get; set; }

        /// <summary>
        /// Optional maximum HP% for fire (1–100). null = no upper-bound gate.
        /// Use case: emergency-only buffs / pots that should only fire when HP drops below a threshold.
        /// </summary>
        public int? MaxHpPercent { get; set; }

        /// <summary>
        /// Optional minimum SP% for fire (1–100). null = no SP gate.
        /// Use case: skip casts when SP is too low to land them (avoids the "no SP" flash spam).
        /// </summary>
        public int? MinSpPercent { get; set; }

        /// <summary>Cycles through skill keys one-per-press so the game's aftercast delay is respected.</summary>
        [JsonIgnore]
        public volatile int currentStep;

        /// <summary>1-based index of the last key that was actually fired. Used by the UI step indicator.</summary>
        [JsonIgnore]
        public volatile int lastFiredStep;

        /// <summary>Environment.TickCount of the last fire in LoopMode. 0 = not yet fired this hold.</summary>
        [JsonIgnore]
        public volatile int lastFireTicks;

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

        /// <summary>
        /// HP/SP percent gates (Tier 3B). Returns true when this slot is allowed to fire under the
        /// current resource state. Fail-open: if max HP/SP read returns 0 (read failed, char select,
        /// not in-game) the gate is skipped — same fail-open posture as <see cref="Client.IsPlayerIdle"/>.
        /// </summary>
        public bool PassesResourceGate(Client client)
        {
            if (this.MinHpPercent == null && this.MaxHpPercent == null && this.MinSpPercent == null)
            {
                return true;
            }

            if (client == null) return true;

            try
            {
                if (this.MinHpPercent != null || this.MaxHpPercent != null)
                {
                    uint maxHp = client.ReadMaxHp();
                    if (maxHp > 0)
                    {
                        uint curHp = client.ReadCurrentHp();
                        // Avoid float math: compare cur*100 vs threshold*max.
                        long curScaled = (long)curHp * 100L;
                        if (this.MinHpPercent is int minHp && curScaled < (long)minHp * maxHp) return false;
                        if (this.MaxHpPercent is int maxHpPct && curScaled > (long)maxHpPct * maxHp) return false;
                    }
                }

                if (this.MinSpPercent is int minSp)
                {
                    uint maxSp = client.ReadMaxSp();
                    if (maxSp > 0)
                    {
                        uint curSp = client.ReadCurrentSp();
                        if ((long)curSp * 100L < (long)minSp * maxSp) return false;
                    }
                }
            }
            catch
            {
                return true; // fail-open on any memory hiccup
            }

            return true;
        }

        /// <summary>
        /// Effective inter-fire delay AFTER the given <paramref name="binding"/> has fired (its aftercast).
        /// Returns the per-skill override when set, otherwise the slot default. Always clamped to the
        /// LoopMode/non-loop floor so a user-entered low value can't bypass <see cref="AHK.LoopModeDelayMinMs"/>.
        /// Pass <c>null</c> to get the slot default (used before the first fire of a hold).
        /// </summary>
        public int GetDelayForBinding(AhkSkillBinding binding)
        {
            int floor = this.LoopMode ? AHK.LoopModeDelayMinMs : AHK.InterSkillDelayMinMs;
            int max = AHK.InterSkillDelayMaxMs;
            int fallback = Math.Max(floor, Math.Min(this.InterSkillDelayMs, max));

            if (binding == null || this.PerSkillDelayMs == null || this.PerSkillDelayMs.Count == 0)
            {
                return fallback;
            }

            string sig = binding.BuildSignature();
            if (string.IsNullOrEmpty(sig)) return fallback;
            if (!this.PerSkillDelayMs.TryGetValue(sig, out int raw) || raw <= 0) return fallback;
            return Math.Max(floor, Math.Min(raw, max));
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

        /// <summary>
        /// Minimum pace for <see cref="AhkSlotConfig.LoopMode"/>. Combined with ~55-100 ms Gaussian hold in
        /// <c>InputSimulator.SendKey</c> this lands near one RO aftercast cycle (~150-180 ms real-world).
        /// Non-loop chain pacing keeps the lower 20 ms floor so existing profiles don't regress.
        /// </summary>
        public const int LoopModeDelayMinMs = 80;

        /// <summary>Maximum pause between keys in bind UI and chain execution (ms).</summary>
        public const int InterSkillDelayMaxMs = 500;

        private _4RThread thread;

        /// <summary>Per-slot: tracks whether the trigger was down on the previous poll (rising-edge detection).</summary>
        [JsonIgnore]
        private readonly bool[] _wasDown = new bool[SLOT_COUNT];

        /// <summary>Set when AHK is stopped so in-flight runs bail out.</summary>
        [JsonIgnore]
        private volatile bool _stopped;

        /// <summary>Per-slot fire worker. Decouples send latency from the poll thread (Tier 2A).</summary>
        [JsonIgnore]
        private Task[] _slotWorkers;

        /// <summary>Per-slot fire signal. Max count 1 — coalesces repeated signals into "there is work".</summary>
        [JsonIgnore]
        private SemaphoreSlim[] _slotSignals;

        /// <summary>Wakes all slot workers when AHK stops.</summary>
        [JsonIgnore]
        private CancellationTokenSource _workerCts;

        /// <summary>Global send lock: serializes worker fires so concurrent PostMessage bursts don't stack.</summary>
        [JsonIgnore]
        private static readonly object _globalSendLock = new object();

        /// <summary>
        /// Minimum gap between any two worker fires across all slots (Tier 2B). Caps total app
        /// PostMessage rate at ~28 Hz so 10 simultaneously-active slots don't paint a visible burst
        /// pattern on the wire. Single-slot users see no effect — InputSimulator's own ~55-100 ms
        /// Gaussian hold already exceeds this gap.
        /// </summary>
        public const int GlobalMinGapMs = 35;

        /// <summary>Upper bound for additional uniform jitter added to the throttle wait.</summary>
        public const int GlobalThrottleJitterMaxMs = 15;

        /// <summary>Environment.TickCount of the last worker fire. Read/written under <see cref="_globalSendLock"/>.</summary>
        [JsonIgnore]
        private static int _lastGlobalSendTick;

        /// <summary>RNG for global throttle jitter. Accessed only under <see cref="_globalSendLock"/>.</summary>
        [JsonIgnore]
        private static readonly Random _globalThrottleRng = new Random();

        // Legacy checkbox mappings kept for backward-compatible profile migration.
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public Dictionary<string, KeyConfig> AhkEntries { get; set; } = new Dictionary<string, KeyConfig>();
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
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
                int minDelay = this.Slots[i].LoopMode ? LoopModeDelayMinMs : InterSkillDelayMinMs;
                this.Slots[i].InterSkillDelayMs = Math.Max(
                    minDelay,
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
            int minDelay = slot.LoopMode ? LoopModeDelayMinMs : InterSkillDelayMinMs;
            slot.InterSkillDelayMs = Math.Max(
                minDelay,
                Math.Min(slot.InterSkillDelayMs, InterSkillDelayMaxMs));

            this.Slots[index] = slot;
        }

        public void Start()
        {
            EnsureSlotsConfigured();

            // Tear down any previous run (poll thread + workers) before starting fresh.
            StopInternal();

            this._stopped = false;

            // Reset all per-slot state (step counters, rising-edge flags, pacing timestamps).
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                _wasDown[i] = false;
            }

            foreach (var slot in this.Slots)
            {
                if (slot != null)
                {
                    slot.currentStep = 0;
                    slot.lastFiredStep = 0;
                    slot.lastFireTicks = 0;
                }
            }

            // Reset global throttle so the first fire after a fresh Start isn't held back by a
            // stale timestamp from a previous run.
            lock (_globalSendLock)
            {
                _lastGlobalSendTick = 0;
            }

            // Build fresh worker infrastructure. One Task per slot with a coalescing semaphore
            // (max count 1). Poll thread signals a semaphore; worker wakes, re-verifies state,
            // and fires under the global send lock.
            _workerCts = new CancellationTokenSource();
            CancellationToken ct = _workerCts.Token;

            _slotSignals = new SemaphoreSlim[SLOT_COUNT];
            _slotWorkers = new Task[SLOT_COUNT];
            for (int i = 0; i < SLOT_COUNT; i++)
            {
                _slotSignals[i] = new SemaphoreSlim(0, 1);
                int captured = i;
                _slotWorkers[i] = Task.Run(() => WorkerLoop(captured, ct), ct);
            }

            this.thread = new _4RThread(_ => AHKThreadExecution());
            _4RThread.Start(this.thread);
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
        /// Poll callback (every 5ms). Rising-edge trigger detection plus optional hold-to-cycle LoopMode.
        /// Non-loop: one press = one fire = one advance. LoopMode: hold cycles keys paced by
        /// <see cref="AhkSlotConfig.InterSkillDelayMs"/> and gated on <see cref="Client.IsPlayerIdle"/>
        /// so we respect the game's aftercast delay.
        /// </summary>
        private int AHKThreadExecution()
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null)
            {
                return 0;
            }

            HashSet<string> handledTriggerSignatures = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < this.Slots.Count && i < SLOT_COUNT; i++)
            {
                if (IsRawKeyDown(FormsKeys.LMenu) || IsRawKeyDown(FormsKeys.RMenu))
                {
                    continue;
                }

                AhkSlotConfig slot = this.Slots[i];
                if (slot == null)
                {
                    _wasDown[i] = false;
                    continue;
                }

                bool isPressed = slot.HasBinding && IsSlotPressed(slot);

                if (!isPressed)
                {
                    _wasDown[i] = false;
                    slot.lastFireTicks = 0;
                    continue;
                }

                if (slot.LoopMode)
                {
                    // Hold-to-cycle: first press fires immediately, then pace by the per-skill delay
                    // for the most recently fired key (its aftercast). Falls back to slot default
                    // when no override is set or before the first fire of this hold.
                    int now = Environment.TickCount;
                    bool firstPress = !_wasDown[i];
                    _wasDown[i] = true;

                    if (!firstPress)
                    {
                        AhkSkillBinding lastFired = null;
                        if (slot.lastFiredStep > 0)
                        {
                            List<AhkSkillBinding> resolved = slot.GetResolvedSkillBindings();
                            if (resolved.Count > 0)
                            {
                                int idx = (slot.lastFiredStep - 1) % resolved.Count;
                                lastFired = resolved[idx];
                            }
                        }

                        int gateMs = slot.GetDelayForBinding(lastFired);
                        if (unchecked(now - slot.lastFireTicks) < gateMs)
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    // Rising edge: only fire on the transition from UP → DOWN.
                    if (_wasDown[i])
                    {
                        continue; // key is still held from a previous press — ignore
                    }

                    _wasDown[i] = true; // mark as down so we don't fire again until released
                }

                // Deduplicate: two slots with same trigger → only first fires.
                string triggerSignature = BuildTriggerSignature(slot);
                if (!string.IsNullOrEmpty(triggerSignature) &&
                    !handledTriggerSignatures.Add(triggerSignature))
                {
                    continue;
                }

                // LoopMode idle gate: skip while the character is walking / casting / attacking so
                // we pace to actual aftercast instead of the fixed ms floor. Fail-open when the
                // pointer chain can't be resolved (IsPlayerIdle returns true). Rising-edge
                // (single-press) path intentionally bypasses this — one press should always fire.
                if (slot.LoopMode && !roClient.IsPlayerIdle())
                {
                    continue;
                }

                // Stamp fire time BEFORE signaling so the next poll tick honors InterSkillDelayMs
                // even while the worker is still holding the global send lock.
                if (slot.LoopMode)
                {
                    slot.lastFireTicks = Environment.TickCount;
                }

                // Hand off to the per-slot worker. Detection stays on this poll thread; the ~55-100 ms
                // Gaussian hold inside SendKey runs on a dedicated Task, so one slow slot can't starve
                // release detection or pacing for the others.
                SignalSlotFire(i);
            }

            return 0;
        }

        /// <summary>
        /// Fires the current key in the slot's cycle and advances the step counter.
        /// Invariant: caller is responsible for idle / busy-state gating. This method always fires
        /// and always advances — do not move <c>IsPlayerIdle</c> checks inside here, otherwise the
        /// step counter will skip whenever the character is busy.
        /// <paramref name="ct"/> propagates worker cancellation into <see cref="InputSimulator.SendKey"/>
        /// so a Stop() during a Gaussian hold wakes immediately (matching WM_KEYUP still posts).
        /// </summary>
        private void FireCurrentStepKey(Client roClient, AhkSlotConfig slot, CancellationToken ct = default)
        {
            if (this._stopped)
            {
                return;
            }

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

            // Wrap around if past the end of the list.
            if (slot.currentStep >= resolved.Count)
            {
                slot.currentStep = 0;
            }

            int fireIndex = slot.currentStep;
            AhkSkillBinding key = resolved[fireIndex];
            int vk = (int)key.ParseKey();

            if (vk != (int)FormsKeys.None)
            {
                bool speedBoost = string.Equals(this.ahkMode, SPEED_BOOST, StringComparison.Ordinal);

                InputAutomationStopProtocol.EnterExclusiveAutomation();
                try
                {
                    FireVanillaSkillStep(live, slot, key, vk, speedBoost, ct);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AHK] Slot {slot.SlotId} key send error: {ex.Message}");
                }
                finally
                {
                    InputAutomationStopProtocol.LeaveExclusiveAutomation();
                }
            }

            slot.lastFiredStep = fireIndex + 1;
            slot.currentStep++;
            Debug.WriteLine($"[AHK] Slot {slot.SlotId} fired step {fireIndex + 1}/{resolved.Count} (key={key?.Key})");
        }

        /// <summary>Matches stock <c>FireOnceWithClick</c> / <c>FireOnceSpeedBoost</c> / <c>FireOnceKeyOnly</c> behavior.</summary>
        private void FireVanillaSkillStep(Client roClient, AhkSlotConfig slot, AhkSkillBinding binding, int vk, bool speedBoost, CancellationToken ct = default)
        {
            if (this.noShift)
            {
                roClient.input.SendKey((int)FormsKeys.ShiftKey, true);
            }

            if (binding.Ctrl || binding.Alt || binding.Shift)
            {
                roClient.input.SendKeyChord(vk, binding.Ctrl, binding.Alt, binding.Shift, false, true, ct);
            }
            else
            {
                roClient.input.SendKey(vk, true, ct);
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

        private bool IsSlotPressed(AhkSlotConfig slot)
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

        private bool IsTriggerBindingPressed(AhkTriggerBinding binding)
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
            bool isDown = IsRawKeyDown(leftKey) || IsRawKeyDown(rightKey);
            return expectedDown == isDown;
        }

        /// <summary>Plain key-down check without numpad mapping. Used for modifiers.</summary>
        private static bool IsRawKeyDown(FormsKeys key)
        {
            return (Native.GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        /// <summary>Bidirectional numpad↔nav key map. NumLock OFF causes Windows to report numpad VKs as nav VKs.</summary>
        private static readonly Dictionary<FormsKeys, FormsKeys> NumpadToNavMap = new Dictionary<FormsKeys, FormsKeys>
        {
            { FormsKeys.NumPad0, FormsKeys.Insert },
            { FormsKeys.NumPad1, FormsKeys.End },
            { FormsKeys.NumPad2, FormsKeys.Down },
            { FormsKeys.NumPad3, FormsKeys.Next },
            { FormsKeys.NumPad4, FormsKeys.Left },
            { FormsKeys.NumPad5, FormsKeys.Clear },
            { FormsKeys.NumPad6, FormsKeys.Right },
            { FormsKeys.NumPad7, FormsKeys.Home },
            { FormsKeys.NumPad8, FormsKeys.Up },
            { FormsKeys.NumPad9, FormsKeys.Prior },
        };
        private static readonly Dictionary<FormsKeys, FormsKeys> NavToNumpadMap =
            NumpadToNavMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        /// <summary>Whether to check paired numpad↔nav VKs. Controlled by UI toggle.</summary>
        public bool numpadMapping { get; set; } = true;

        private bool IsKeyDown(FormsKeys key)
        {
            if ((Native.GetAsyncKeyState((int)key) & 0x8000) != 0)
                return true;

            if (!numpadMapping)
                return false;

            // NumLock off: numpad keys report as nav keys (and vice versa).
            // Check the paired VK so detection works regardless of NumLock state.
            FormsKeys alt;
            if (NumpadToNavMap.TryGetValue(key, out alt) || NavToNumpadMap.TryGetValue(key, out alt))
                return (Native.GetAsyncKeyState((int)alt) & 0x8000) != 0;

            return false;
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
            StopInternal();
        }

        /// <summary>
        /// Full teardown: cancels worker token, releases semaphores so workers wake and observe
        /// cancellation, stops the poll thread, joins workers with a 500 ms timeout, and clears
        /// worker infrastructure. Safe to call when nothing is running.
        /// </summary>
        private void StopInternal()
        {
            this._stopped = true;

            CancellationTokenSource cts = _workerCts;
            if (cts != null)
            {
                try { cts.Cancel(); }
                catch (ObjectDisposedException) { }
            }

            SemaphoreSlim[] sigs = _slotSignals;
            if (sigs != null)
            {
                for (int i = 0; i < sigs.Length; i++)
                {
                    SemaphoreSlim sem = sigs[i];
                    if (sem == null) continue;
                    try
                    {
                        if (sem.CurrentCount == 0) sem.Release();
                    }
                    catch (SemaphoreFullException) { }
                    catch (ObjectDisposedException) { }
                }
            }

            if (this.thread != null)
            {
                _4RThread.Stop(this.thread);
            }

            Task[] workers = _slotWorkers;
            if (workers != null)
            {
                try
                {
                    Task[] nonNull = workers.Where(t => t != null).ToArray();
                    if (nonNull.Length > 0)
                    {
                        Task.WaitAll(nonNull, 500);
                    }
                }
                catch (AggregateException) { /* OperationCanceledException is expected */ }
            }

            // Drop references; let GC clean up semaphores / CTS. Avoiding Dispose here prevents
            // ObjectDisposedException races if a worker missed cancellation.
            _workerCts = null;
            _slotSignals = null;
            _slotWorkers = null;

            for (int i = 0; i < SLOT_COUNT; i++)
            {
                _wasDown[i] = false;
            }
        }

        /// <summary>
        /// Sleeps the calling worker until at least <see cref="GlobalMinGapMs"/> + small jitter has
        /// elapsed since the last global fire. Caller MUST hold <see cref="_globalSendLock"/>.
        /// First fire (timestamp 0) and TickCount wraparound (negative elapsed) skip the wait.
        /// </summary>
        private void ApplyGlobalThrottle()
        {
            int last = _lastGlobalSendTick;
            if (last == 0) return;

            int elapsed = unchecked(Environment.TickCount - last);
            if (elapsed < 0 || elapsed >= GlobalMinGapMs) return;

            int jitter = _globalThrottleRng.Next(0, GlobalThrottleJitterMaxMs + 1);
            int wait = (GlobalMinGapMs - elapsed) + jitter;

            // SleepWhileRunning chunks at 15 ms and bails on _stopped.
            SleepWhileRunning(wait);
        }

        /// <summary>Wake the worker for <paramref name="slotIndex"/> if it is idle. No-op if already signalled.</summary>
        private void SignalSlotFire(int slotIndex)
        {
            SemaphoreSlim[] sigs = _slotSignals;
            if (sigs == null || slotIndex < 0 || slotIndex >= sigs.Length) return;

            SemaphoreSlim sem = sigs[slotIndex];
            if (sem == null) return;

            try
            {
                if (sem.CurrentCount == 0)
                {
                    sem.Release();
                }
            }
            catch (SemaphoreFullException) { /* coalesced: a pending fire is already queued */ }
            catch (ObjectDisposedException) { /* raced with Stop — ignore */ }
        }

        /// <summary>
        /// Per-slot worker loop. Awaits its signal, re-verifies slot state and idle gate, then
        /// fires under the global send lock so concurrent workers don't stack PostMessage bursts.
        /// Exits on cancellation or <see cref="_stopped"/>.
        /// </summary>
        private void WorkerLoop(int slotIndex, CancellationToken ct)
        {
            SemaphoreSlim[] sigs = _slotSignals;
            SemaphoreSlim sem = (sigs != null && slotIndex < sigs.Length) ? sigs[slotIndex] : null;
            if (sem == null) return;

            while (!ct.IsCancellationRequested && !_stopped)
            {
                try
                {
                    sem.Wait(ct);
                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }

                if (ct.IsCancellationRequested || _stopped) return;

                if (slotIndex >= this.Slots.Count) continue;
                AhkSlotConfig slot = this.Slots[slotIndex];
                if (slot == null) continue;

                Client roClient = ClientSingleton.GetClient();
                if (roClient == null) continue;

                // Defensive re-check: trigger may have been released between signal and wake.
                if (!slot.HasBinding || !IsSlotPressed(slot)) continue;

                // Re-check idle gate at fire time (state may have changed since poll thread signalled).
                if (slot.LoopMode && !roClient.IsPlayerIdle()) continue;

                // Resource gates (Tier 3B): skip the fire if HP/SP conditions aren't met.
                // Fail-open inside PassesResourceGate when memory reads return 0.
                if (!slot.PassesResourceGate(roClient)) continue;

                lock (_globalSendLock)
                {
                    if (_stopped || ct.IsCancellationRequested) return;

                    ApplyGlobalThrottle();

                    FireCurrentStepKey(roClient, slot, ct);

                    _lastGlobalSendTick = Environment.TickCount;
                }
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
