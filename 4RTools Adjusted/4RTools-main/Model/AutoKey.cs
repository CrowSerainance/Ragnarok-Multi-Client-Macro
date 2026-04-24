using System;
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

        private _4RThread thread;

        public string ActionName { get; set; } = ACTION_NAME;

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

        [JsonProperty("IntervalMinutes")]
        public int IntervalMinutes { get; set; } = 1;

        [JsonProperty("IntervalSeconds")]
        public int IntervalSeconds { get; set; } = 0;

        public AutoKey() { }

        public AutoKey(string actionName)
        {
            this.ActionName = actionName;
        }

        public int GetIntervalMs()
        {
            int minutes = Math.Max(0, Math.Min(60, this.IntervalMinutes));
            int seconds = Math.Max(0, Math.Min(59, this.IntervalSeconds));
            int totalMs = (minutes * 60 + seconds) * 1000;
            return totalMs <= 0 ? 1000 : totalMs;
        }

        public void Start()
        {
            Client roClient = ClientSingleton.GetClient();
            if (roClient == null) return;
            if (this.Key == Key.None) return;

            int intervalMs = GetIntervalMs();
            this.thread = new _4RThread(_ => ThreadExecution(roClient, intervalMs));
            _4RThread.Start(this.thread);
        }

        private int ThreadExecution(Client roClient, int intervalMs)
        {
            int vkCode;
            try
            {
                vkCode = (int)Enum.Parse(typeof(Keys), this.Key.ToString());
            }
            catch
            {
                Thread.Sleep(intervalMs);
                return 0;
            }

            try
            {
                if (this.UseChord && (this.Ctrl || this.Alt || this.Shift || this.Win))
                {
                    roClient.input.SendKeyChord(vkCode, this.Ctrl, this.Alt, this.Shift, this.Win);
                }
                else
                {
                    roClient.input.SendKey(vkCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoKey] Send failed: {ex.Message}");
            }

            Thread.Sleep(intervalMs);
            return 0;
        }

        public void Stop()
        {
            _4RThread.Stop(this.thread);
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
}
