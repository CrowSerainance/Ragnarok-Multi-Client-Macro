using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;
using _4RTools.Utils.MuhBotCore;
using FormsKeys = System.Windows.Forms.Keys;

namespace _4RTools.Forms
{
    public partial class AHKForm : Form, IObserver
    {
        private readonly List<SlotRowControls> slotRows = new List<SlotRowControls>();
        private bool updatingUi;

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            InitializeSlotRows();
            subject.Attach(this);
            UpdateUI();
        }

        public void Update(ISubject subject)
        {
            switch ((subject as Subject).Message.code)
            {
                case MessageCode.PROFILE_CHANGED:
                    UpdateUI();
                    break;
                case MessageCode.TURN_ON:
                    ProfileSingleton.GetCurrent().AHK.Start();
                    break;
                case MessageCode.TURN_OFF:
                    ProfileSingleton.GetCurrent().AHK.Stop();
                    break;
            }
        }

        private void UpdateUI()
        {
            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.EnsureSlotsConfigured();

            updatingUi = true;
            try
            {
                RadioButton currentMode = (RadioButton)this.groupAhkConfig.Controls[ahk.ahkMode];
                if (currentMode != null)
                {
                    currentMode.Checked = true;
                }

                decimal delay = ahk.AhkDelay;
                if (delay < this.txtSpammerDelay.Minimum) delay = this.txtSpammerDelay.Minimum;
                if (delay > this.txtSpammerDelay.Maximum) delay = this.txtSpammerDelay.Maximum;
                this.txtSpammerDelay.Value = delay;
                this.chkNoShift.Checked = ahk.noShift;
                this.chkMouseFlick.Checked = ahk.mouseFlick;
                this.DisableControlsIfSpeedBoost();

                for (int i = 0; i < slotRows.Count; i++)
                {
                    AhkSlotConfig slot = ahk.Slots[i];
                    SlotRowControls row = slotRows[i];
                    row.EnabledCheckBox.Checked = slot.Enabled;
                    row.ClickCheckBox.Checked = slot.ClickActive;
                    row.BindingTextBox.Text = FormatBinding(slot);
                }
            }
            finally
            {
                updatingUi = false;
            }
        }

        private void InitializeSlotRows()
        {
            AddHeaderRow();

            for (int i = 0; i < AHK.SLOT_COUNT; i++)
            {
                int slotIndex = i;

                Panel rowPanel = new Panel
                {
                    Location = new Point(0, 24 + (i * 28)),
                    Size = new Size(510, 26),
                    Margin = new Padding(0)
                };

                Label slotLabel = new Label
                {
                    AutoSize = false,
                    Location = new Point(8, 5),
                    Size = new Size(42, 18),
                    Text = $"S{i + 1:00}"
                };

                TextBox bindingTextBox = new TextBox
                {
                    Location = new Point(56, 2),
                    Size = new Size(202, 20),
                    ReadOnly = true,
                    TabStop = false
                };

                Button bindButton = new Button
                {
                    Location = new Point(264, 1),
                    Size = new Size(52, 22),
                    Text = "Bind",
                    Tag = slotIndex
                };
                bindButton.Click += this.BindButton_Click;

                CheckBox clickCheckBox = new CheckBox
                {
                    AutoSize = true,
                    Location = new Point(328, 4),
                    Text = "Click",
                    Tag = slotIndex
                };
                clickCheckBox.CheckedChanged += this.ClickCheckBox_CheckedChanged;

                CheckBox enabledCheckBox = new CheckBox
                {
                    AutoSize = true,
                    Location = new Point(392, 4),
                    Text = "On",
                    Tag = slotIndex
                };
                enabledCheckBox.CheckedChanged += this.EnabledCheckBox_CheckedChanged;

                Button clearButton = new Button
                {
                    Location = new Point(446, 1),
                    Size = new Size(52, 22),
                    Text = "Clear",
                    Tag = slotIndex
                };
                clearButton.Click += this.ClearButton_Click;

                rowPanel.Controls.Add(slotLabel);
                rowPanel.Controls.Add(bindingTextBox);
                rowPanel.Controls.Add(bindButton);
                rowPanel.Controls.Add(clickCheckBox);
                rowPanel.Controls.Add(enabledCheckBox);
                rowPanel.Controls.Add(clearButton);
                this.panelSlots.Controls.Add(rowPanel);

                this.slotRows.Add(new SlotRowControls
                {
                    BindingTextBox = bindingTextBox,
                    ClickCheckBox = clickCheckBox,
                    EnabledCheckBox = enabledCheckBox
                });
            }
        }

        private void AddHeaderRow()
        {
            Panel headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(510, 22),
                Margin = new Padding(0)
            };

            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(8, 3), Size = new Size(42, 16), Text = "Slot" });
            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(56, 3), Size = new Size(120, 16), Text = "Binding" });
            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(328, 3), Size = new Size(40, 16), Text = "Mode" });
            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(392, 3), Size = new Size(24, 16), Text = "Use" });

            this.panelSlots.Controls.Add(headerPanel);
        }

        private void BindButton_Click(object sender, EventArgs e)
        {
            int slotIndex = (int)((Control)sender).Tag;
            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.EnsureSlotsConfigured();

            using (BindingCaptureDialog dialog = new BindingCaptureDialog(ahk.Slots[slotIndex]))
            {
                DialogResult result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    AhkSlotConfig slot = ahk.Slots[slotIndex];
                    dialog.ApplyToSlot(slot);
                    PersistAhkConfiguration();
                    UpdateUI();
                }
                else if (result == DialogResult.Abort)
                {
                    ClearSlot(slotIndex);
                }
            }
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            int slotIndex = (int)((Control)sender).Tag;
            ClearSlot(slotIndex);
        }

        private void ClearSlot(int slotIndex)
        {
            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.EnsureSlotsConfigured();

            AhkSlotConfig slot = ahk.Slots[slotIndex];
            slot.TriggerBindings = new List<AhkTriggerBinding>();
            slot.SkillKeys = new List<string>();
            slot.TriggerKey = FormsKeys.None.ToString();
            slot.SkillKey = FormsKeys.None.ToString();
            slot.Ctrl = false;
            slot.Alt = false;
            slot.Shift = false;
            slot.Win = false;
            slot.Enabled = false;
            slot.EnsureSlotModelConsistent();

            PersistAhkConfiguration();
            UpdateUI();
        }

        private void ClickCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            int slotIndex = (int)((Control)sender).Tag;
            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.EnsureSlotsConfigured();
            ahk.Slots[slotIndex].ClickActive = ((CheckBox)sender).Checked;
            PersistAhkConfiguration();
        }

        private void EnabledCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            int slotIndex = (int)((Control)sender).Tag;
            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.EnsureSlotsConfigured();

            AhkSlotConfig slot = ahk.Slots[slotIndex];
            slot.EnsureSlotModelConsistent();
            bool shouldEnable = ((CheckBox)sender).Checked &&
                                slot.ParseTriggerKey() != FormsKeys.None &&
                                slot.GetResolvedSkillKeyCodes().Count > 0;
            slot.Enabled = shouldEnable;

            PersistAhkConfiguration();
            UpdateUI();
        }

        private void txtSpammerDelay_TextChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AHK.AhkDelay = decimal.ToInt16(this.txtSpammerDelay.Value);
            PersistAhkConfiguration();
        }

        private void RadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            RadioButton rb = sender as RadioButton;
            if (rb.Checked)
            {
                ProfileSingleton.GetCurrent().AHK.ahkMode = rb.Name;
                PersistAhkConfiguration();
                this.DisableControlsIfSpeedBoost();
            }
        }

        private void chkMouseFlick_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AHK.mouseFlick = this.chkMouseFlick.Checked;
            PersistAhkConfiguration();
        }

        private void chkNoShift_CheckedChanged(object sender, EventArgs e)
        {
            if (updatingUi)
            {
                return;
            }

            ProfileSingleton.GetCurrent().AHK.noShift = this.chkNoShift.Checked;
            PersistAhkConfiguration();
        }

        private void DisableControlsIfSpeedBoost()
        {
            bool speedBoost = ProfileSingleton.GetCurrent().AHK.ahkMode == AHK.SPEED_BOOST;
            this.chkMouseFlick.Enabled = !speedBoost;
            this.chkNoShift.Enabled = !speedBoost;
        }

        private static string FormatBinding(AhkSlotConfig slot)
        {
            slot.EnsureSlotModelConsistent();
            if (slot.ParseTriggerKey() == FormsKeys.None || slot.GetResolvedSkillKeyCodes().Count == 0)
            {
                return "Not bound";
            }

            AhkTriggerBinding trig = new AhkTriggerBinding
            {
                TriggerKey = slot.TriggerKey,
                Ctrl = slot.Ctrl,
                Alt = slot.Alt,
                Shift = slot.Shift,
                Win = slot.Win
            };

            IEnumerable<string> skillSources = slot.SkillKeys != null && slot.SkillKeys.Count > 0
                ? slot.SkillKeys
                : new[] { slot.SkillKey };

            List<string> skillLabels = new List<string>();
            foreach (string s in skillSources)
            {
                FormsKeys k = AhkSlotConfig.ParseKeyStringStatic(s);
                if (k != FormsKeys.None)
                {
                    skillLabels.Add(GetFriendlyKeyName(k));
                }
            }

            if (skillLabels.Count == 0)
            {
                return "Not bound";
            }

            return $"{FormatOneTrigger(trig)}  →  {string.Join(" · ", skillLabels)}";
        }

        private static string FormatOneTrigger(AhkTriggerBinding binding)
        {
            FormsKeys key = binding.ParseKey();
            if (key == FormsKeys.None)
            {
                return "?";
            }

            List<string> parts = new List<string>(5);
            if (binding.Ctrl) parts.Add("Ctrl");
            if (binding.Alt) parts.Add("Alt");
            if (binding.Shift) parts.Add("Shift");
            if (binding.Win) parts.Add("Win");
            parts.Add(GetFriendlyKeyName(key));

            return string.Join(" + ", parts);
        }

        private static string GetFriendlyKeyName(FormsKeys key)
        {
            switch (key)
            {
                case FormsKeys.Prior:
                    return "PageUp";
                case FormsKeys.Next:
                    return "PageDown";
                case FormsKeys.Return:
                    return "Enter";
                case FormsKeys.OemMinus:
                    return "-";
                case FormsKeys.Oemplus:
                    return "=";
                case FormsKeys.OemOpenBrackets:
                    return "[";
                case FormsKeys.OemCloseBrackets:
                    return "]";
                case FormsKeys.OemPipe:
                    return "\\";
                case FormsKeys.OemSemicolon:
                    return ";";
                case FormsKeys.Oemtilde:
                    return "`";
                case FormsKeys.OemQuotes:
                    return "'";
                case FormsKeys.Oemcomma:
                    return ",";
                case FormsKeys.OemPeriod:
                    return ".";
                case FormsKeys.OemQuestion:
                    return "/";
            }

            if (key >= FormsKeys.D0 && key <= FormsKeys.D9)
            {
                return ((int)(key - FormsKeys.D0)).ToString();
            }

            if (key >= FormsKeys.NumPad0 && key <= FormsKeys.NumPad9)
            {
                return $"Num{(int)(key - FormsKeys.NumPad0)}";
            }

            return key.ToString();
        }

        private static bool IsModifierOnlyKey(FormsKeys key)
        {
            switch (key)
            {
                case FormsKeys.ControlKey:
                case FormsKeys.LControlKey:
                case FormsKeys.RControlKey:
                case FormsKeys.ShiftKey:
                case FormsKeys.LShiftKey:
                case FormsKeys.RShiftKey:
                case FormsKeys.Menu:
                case FormsKeys.LMenu:
                case FormsKeys.RMenu:
                case FormsKeys.LWin:
                case FormsKeys.RWin:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsWinCurrentlyPressed()
        {
            return (Native.GetAsyncKeyState((int)FormsKeys.LWin) & 0x8000) != 0 ||
                   (Native.GetAsyncKeyState((int)FormsKeys.RWin) & 0x8000) != 0;
        }

        private static void PersistAhkConfiguration()
        {
            ProfileSingleton.GetCurrent().AHK.EnsureSlotsConfigured();
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
        }

        private sealed class SlotRowControls
        {
            public TextBox BindingTextBox { get; set; }
            public CheckBox ClickCheckBox { get; set; }
            public CheckBox EnabledCheckBox { get; set; }
        }

        private sealed class BindingCaptureDialog : Form
        {
            private enum ListenMode
            {
                None,
                AddSkill,
                SetTrigger
            }

            private readonly TextBox txtTrigger;
            private readonly ListBox listSkills;
            private readonly Label lblStatus;
            private readonly Button btnOk;
            private AhkTriggerBinding workingTrigger;
            private readonly List<string> workingSkills;
            private ListenMode listenMode = ListenMode.None;

            public BindingCaptureDialog(AhkSlotConfig current)
            {
                current.EnsureSlotModelConsistent();
                this.workingSkills = current.SkillKeys != null
                    ? new List<string>(current.SkillKeys)
                    : new List<string>();
                if (this.workingSkills.Count == 0 && current.ParseSkillKey() != FormsKeys.None)
                {
                    this.workingSkills.Add(current.SkillKey);
                }

                this.workingTrigger = new AhkTriggerBinding
                {
                    TriggerKey = current.TriggerKey,
                    Ctrl = current.Ctrl,
                    Alt = current.Alt,
                    Shift = current.Shift,
                    Win = current.Win
                };

                this.Text = "Bind Key";
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ClientSize = new Size(400, 340);
                this.KeyPreview = true;
                this.ShowInTaskbar = false;

                Label lblTrigger = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 12),
                    Text = "Trigger shortcut (hold to activate):"
                };

                this.txtTrigger = new TextBox
                {
                    Location = new Point(12, 32),
                    Size = new Size(260, 22),
                    ReadOnly = true,
                    TabStop = false
                };

                Button btnSetTrigger = new Button
                {
                    Location = new Point(280, 30),
                    Size = new Size(100, 24),
                    Text = "Set trigger..."
                };
                btnSetTrigger.Click += (_, __) => this.BeginListen(ListenMode.SetTrigger);

                Label lblSkills = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 64),
                    Text = "Skill keys (sent to game, cycled each press):"
                };

                this.listSkills = new ListBox
                {
                    Location = new Point(12, 84),
                    Size = new Size(368, 120),
                    IntegralHeight = false
                };

                Button btnAddSkill = new Button
                {
                    Location = new Point(12, 210),
                    Size = new Size(120, 24),
                    Text = "Add skill..."
                };
                btnAddSkill.Click += (_, __) => this.BeginListen(ListenMode.AddSkill);

                Button btnRemove = new Button
                {
                    Location = new Point(140, 210),
                    Size = new Size(80, 24),
                    Text = "Remove"
                };
                btnRemove.Click += this.RemoveSelectedSkill;

                this.lblStatus = new Label
                {
                    AutoSize = false,
                    Location = new Point(12, 240),
                    Size = new Size(368, 36),
                    Text = "Set one trigger, then add skill keys (A–Z, 0–9, -, =, …). Esc cancels capture or closes."
                };

                Button btnClear = new Button
                {
                    DialogResult = DialogResult.Abort,
                    Location = new Point(12, 300),
                    Size = new Size(70, 24),
                    Text = "Clear"
                };

                Button btnCancel = new Button
                {
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(230, 300),
                    Size = new Size(70, 24),
                    Text = "Cancel"
                };

                this.btnOk = new Button
                {
                    Location = new Point(310, 300),
                    Size = new Size(70, 24),
                    Text = "OK"
                };
                this.btnOk.Click += this.OnOkClick;

                this.Controls.Add(lblTrigger);
                this.Controls.Add(this.txtTrigger);
                this.Controls.Add(btnSetTrigger);
                this.Controls.Add(lblSkills);
                this.Controls.Add(this.listSkills);
                this.Controls.Add(btnAddSkill);
                this.Controls.Add(btnRemove);
                this.Controls.Add(this.lblStatus);
                this.Controls.Add(btnClear);
                this.Controls.Add(btnCancel);
                this.Controls.Add(this.btnOk);
                this.AcceptButton = this.btnOk;
                this.CancelButton = btnCancel;

                this.RefreshDisplays();
            }

            public void ApplyToSlot(AhkSlotConfig slot)
            {
                slot.SkillKeys = this.workingSkills.ToList();
                slot.TriggerKey = this.workingTrigger.TriggerKey;
                slot.Ctrl = this.workingTrigger.Ctrl;
                slot.Alt = this.workingTrigger.Alt;
                slot.Shift = this.workingTrigger.Shift;
                slot.Win = this.workingTrigger.Win;
                slot.TriggerBindings = new List<AhkTriggerBinding>
                {
                    new AhkTriggerBinding
                    {
                        TriggerKey = this.workingTrigger.TriggerKey,
                        Ctrl = this.workingTrigger.Ctrl,
                        Alt = this.workingTrigger.Alt,
                        Shift = this.workingTrigger.Shift,
                        Win = this.workingTrigger.Win
                    }
                };
                slot.EnsureSlotModelConsistent();
                slot.Enabled = slot.ParseTriggerKey() != FormsKeys.None &&
                               slot.GetResolvedSkillKeyCodes().Count > 0;
            }

            private void BeginListen(ListenMode mode)
            {
                this.listenMode = mode;
                this.lblStatus.Text = mode == ListenMode.AddSkill
                    ? "Press the key to add as a skill (main key only; Esc cancels)."
                    : "Press the trigger shortcut (Ctrl/Alt/Shift/Win allowed). Esc cancels capture.";
            }

            private void RemoveSelectedSkill(object sender, EventArgs e)
            {
                int i = this.listSkills.SelectedIndex;
                if (i >= 0 && i < this.workingSkills.Count)
                {
                    this.workingSkills.RemoveAt(i);
                    this.RefreshDisplays();
                }
            }

            private void RefreshDisplays()
            {
                FormsKeys tk = this.workingTrigger.ParseKey();
                this.txtTrigger.Text = tk == FormsKeys.None ? "(not set)" : FormatOneTrigger(this.workingTrigger);

                this.listSkills.Items.Clear();
                foreach (string s in this.workingSkills)
                {
                    FormsKeys k = AhkSlotConfig.ParseKeyStringStatic(s);
                    this.listSkills.Items.Add(k == FormsKeys.None ? s : GetFriendlyKeyName(k));
                }

                this.listenMode = ListenMode.None;
                this.lblStatus.Text =
                    "Set one trigger, then add skill keys (A–Z, 0–9, -, =, …). Esc cancels capture or closes.";
            }

            private void OnOkClick(object sender, EventArgs e)
            {
                if (this.workingTrigger.ParseKey() == FormsKeys.None ||
                    !this.workingSkills.Any(s => AhkSlotConfig.ParseKeyStringStatic(s) != FormsKeys.None))
                {
                    MessageBox.Show(
                        this,
                        "Set a trigger shortcut and at least one valid skill key.",
                        "Bind Key",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                this.DialogResult = DialogResult.OK;
                this.Close();
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);

                FormsKeys keyCode = e.KeyCode & FormsKeys.KeyCode;
                if (keyCode == FormsKeys.Escape)
                {
                    if (this.listenMode != ListenMode.None)
                    {
                        this.listenMode = ListenMode.None;
                        this.RefreshDisplays();
                        e.SuppressKeyPress = true;
                        return;
                    }

                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                if (this.listenMode == ListenMode.None)
                {
                    return;
                }

                if (IsModifierOnlyKey(keyCode))
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                if (this.listenMode == ListenMode.AddSkill)
                {
                    this.workingSkills.Add(keyCode.ToString());
                    this.listenMode = ListenMode.None;
                    this.RefreshDisplays();
                    e.SuppressKeyPress = true;
                    return;
                }

                this.workingTrigger = new AhkTriggerBinding
                {
                    TriggerKey = keyCode.ToString(),
                    Ctrl = e.Control,
                    Alt = e.Alt,
                    Shift = e.Shift,
                    Win = IsWinCurrentlyPressed()
                };

                this.listenMode = ListenMode.None;
                this.RefreshDisplays();
                e.SuppressKeyPress = true;
            }
        }
    }
}
