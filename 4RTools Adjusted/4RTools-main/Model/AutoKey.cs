using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;
using Newtonsoft.Json;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class AutoKey : Action
    {
        public const string ACTION_NAME = "AutoKey";
        public const int SLOT_COUNT = 5;

        [JsonProperty("ActionName")]
        public string ActionName { get; set; } = ACTION_NAME;

        [JsonProperty("Slots")]
        public List<AutoKeySlot> Slots { get; set; } = new List<AutoKeySlot>();

        public AutoKey() { EnsureSlots(); }

        public AutoKey(string actionName)
        {
            this.ActionName = actionName;
            EnsureSlots();
        }

        public void EnsureSlots()
        {
            if (Slots == null) Slots = new List<AutoKeySlot>();
            while (Slots.Count < SLOT_COUNT) Slots.Add(new AutoKeySlot());
            if (Slots.Count > SLOT_COUNT) Slots.RemoveRange(SLOT_COUNT, Slots.Count - SLOT_COUNT);
            foreach (AutoKeySlot s in Slots) s.EnsureBindings();
        }

        public void Start()
        {
            EnsureSlots();
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null) return;
            foreach (AutoKeySlot slot in Slots) slot.Start(roClient);
        }

        public void Stop()
        {
            if (Slots == null) return;
            foreach (AutoKeySlot slot in Slots) slot.Stop();
        }

        public string GetConfiguration()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string GetActionName()
        {
            return this.ActionName;
        }
    }

    public class AutoKeySlot
    {
        private _4RThread thread;

        [JsonProperty("Enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("Bindings")]
        public List<AutoKeyBinding> Bindings { get; set; } = new List<AutoKeyBinding>();

        [JsonProperty("StepDelayMs")]
        public int StepDelayMs { get; set; } = 150;

        [JsonProperty("IntervalMinutes")]
        public int IntervalMinutes { get; set; } = 1;

        [JsonProperty("IntervalSeconds")]
        public int IntervalSeconds { get; set; }

        public void EnsureBindings()
        {
            if (Bindings == null) Bindings = new List<AutoKeyBinding>();
        }

        public int GetIntervalMs()
        {
            int m = Math.Max(0, Math.Min(60, IntervalMinutes));
            int s = Math.Max(0, Math.Min(59, IntervalSeconds));
            int total = (m * 60 + s) * 1000;
            return total <= 0 ? 1000 : total;
        }

        public int GetStepDelayMs()
        {
            if (StepDelayMs < 10) return 10;
            if (StepDelayMs > 5000) return 5000;
            return StepDelayMs;
        }

        public void Start(Client roClient)
        {
            EnsureBindings();
            if (!Enabled) return;
            if (Bindings.Count == 0) return;

            int intervalMs = GetIntervalMs();
            int stepMs = GetStepDelayMs();

            this.thread = new _4RThread(_ => Tick(roClient, intervalMs, stepMs));
            _4RThread.Start(this.thread);
        }

        private int Tick(Client roClient, int intervalMs, int stepMs)
        {
            for (int i = 0; i < Bindings.Count; i++)
            {
                AutoKeyBinding b = Bindings[i];
                if (b == null || b.Key == Key.None) continue;

                int vk;
                try { vk = (int)Enum.Parse(typeof(Keys), b.Key.ToString()); }
                catch { continue; }

                try
                {
                    if (b.UseChord && (b.Ctrl || b.Alt || b.Shift || b.Win))
                        roClient.input.SendKeyChord(vk, b.Ctrl, b.Alt, b.Shift, b.Win);
                    else
                        roClient.input.SendKey(vk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoKeySlot] Send failed: {ex.Message}");
                }

                if (i < Bindings.Count - 1) Thread.Sleep(stepMs);
            }

            Thread.Sleep(intervalMs);
            return 0;
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
            this.thread = null;
        }
    }

    public class AutoKeyBinding
    {
        [JsonProperty("Key")]
        public Key Key { get; set; } = Key.None;

        [JsonProperty("UseChord")]
        public bool UseChord { get; set; }

        [JsonProperty("Ctrl")]
        public bool Ctrl { get; set; }

        [JsonProperty("Alt")]
        public bool Alt { get; set; }

        [JsonProperty("Shift")]
        public bool Shift { get; set; }

        [JsonProperty("Win")]
        public bool Win { get; set; }

        public string Display()
        {
            if (Key == Key.None) return "(empty)";
            if (!UseChord || !(Ctrl || Alt || Shift || Win))
                return Key.ToString();

            List<string> parts = new List<string>();
            if (Ctrl) parts.Add("Ctrl");
            if (Alt) parts.Add("Alt");
            if (Shift) parts.Add("Shift");
            if (Win) parts.Add("Win");
            parts.Add(Key.ToString());
            return string.Join("+", parts);
        }

        public AutoKeyBinding Clone()
        {
            return new AutoKeyBinding
            {
                Key = this.Key,
                UseChord = this.UseChord,
                Ctrl = this.Ctrl,
                Alt = this.Alt,
                Shift = this.Shift,
                Win = this.Win
            };
        }
    }
}
