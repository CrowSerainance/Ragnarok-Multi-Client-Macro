using System;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AutoKeyForm : Form, IObserver
    {
        private bool suppressEvents;

        public AutoKeyForm(Subject subject)
        {
            InitializeComponent();
            subject.Attach(this);

            this.txtAutoKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            this.txtAutoKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            this.txtAutoKey.TextChanged += new EventHandler(this.OnKeyChanged);

            this.chkUseChord.CheckedChanged += new EventHandler(this.OnUseChordChanged);
            this.chkCtrl.CheckedChanged += new EventHandler(this.OnModifierChanged);
            this.chkAlt.CheckedChanged += new EventHandler(this.OnModifierChanged);
            this.chkShift.CheckedChanged += new EventHandler(this.OnModifierChanged);
            this.chkWin.CheckedChanged += new EventHandler(this.OnModifierChanged);

            this.nudMinutes.ValueChanged += new EventHandler(this.OnIntervalChanged);
            this.nudSeconds.ValueChanged += new EventHandler(this.OnIntervalChanged);
        }

        public void Update(ISubject subject)
        {
            switch ((subject as Subject).Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    SyncFromProfile();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AutoKey.Start();
                    SetEditingEnabled(false);
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AutoKey.Stop();
                    SetEditingEnabled(true);
                    break;
            }
        }

        private void SyncFromProfile()
        {
            AutoKey ak = ProfileSingleton.GetCurrent().AutoKey;
            if (ak == null) return;

            suppressEvents = true;
            try
            {
                this.txtAutoKey.Text = ak.Key == Key.None ? "" : ak.Key.ToString();
                this.chkUseChord.Checked = ak.UseChord;
                this.chkCtrl.Checked = ak.Ctrl;
                this.chkAlt.Checked = ak.Alt;
                this.chkShift.Checked = ak.Shift;
                this.chkWin.Checked = ak.Win;
                this.nudMinutes.Value = Math.Max(this.nudMinutes.Minimum, Math.Min(this.nudMinutes.Maximum, ak.IntervalMinutes));
                this.nudSeconds.Value = Math.Max(this.nudSeconds.Minimum, Math.Min(this.nudSeconds.Maximum, ak.IntervalSeconds));
                ApplyChordEnabled();
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private void SetEditingEnabled(bool enabled)
        {
            this.txtAutoKey.Enabled = enabled;
            this.chkUseChord.Enabled = enabled;
            this.nudMinutes.Enabled = enabled;
            this.nudSeconds.Enabled = enabled;
            ApplyChordEnabled();
            if (!enabled)
            {
                this.chkCtrl.Enabled = false;
                this.chkAlt.Enabled = false;
                this.chkShift.Enabled = false;
                this.chkWin.Enabled = false;
            }
        }

        private void ApplyChordEnabled()
        {
            bool on = this.chkUseChord.Checked && this.chkUseChord.Enabled;
            this.chkCtrl.Enabled = on;
            this.chkAlt.Enabled = on;
            this.chkShift.Enabled = on;
            this.chkWin.Enabled = on;
        }

        private void OnKeyChanged(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            string text = this.txtAutoKey.Text;
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                Key key = (Key)Enum.Parse(typeof(Key), text);
                ProfileSingleton.GetCurrent().AutoKey.Key = key;
                ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AutoKey);
            }
            catch
            {
                // unknown key string — ignore
            }
        }

        private void OnUseChordChanged(object sender, EventArgs e)
        {
            ApplyChordEnabled();
            if (suppressEvents) return;
            ProfileSingleton.GetCurrent().AutoKey.UseChord = this.chkUseChord.Checked;
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AutoKey);
        }

        private void OnModifierChanged(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            AutoKey ak = ProfileSingleton.GetCurrent().AutoKey;
            ak.Ctrl = this.chkCtrl.Checked;
            ak.Alt = this.chkAlt.Checked;
            ak.Shift = this.chkShift.Checked;
            ak.Win = this.chkWin.Checked;
            ProfileSingleton.SetConfiguration(ak);
        }

        private void OnIntervalChanged(object sender, EventArgs e)
        {
            if (suppressEvents) return;
            AutoKey ak = ProfileSingleton.GetCurrent().AutoKey;
            ak.IntervalMinutes = (int)this.nudMinutes.Value;
            ak.IntervalSeconds = (int)this.nudSeconds.Value;
            ProfileSingleton.SetConfiguration(ak);
        }
    }
}
