using System;
using System.Collections.Generic;
using System.Drawing;
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
                    slot.TriggerKey = dialog.CapturedBinding.TriggerKey;
                    slot.Ctrl = dialog.CapturedBinding.Ctrl;
                    slot.Alt = dialog.CapturedBinding.Alt;
                    slot.Shift = dialog.CapturedBinding.Shift;
                    slot.Win = dialog.CapturedBinding.Win;
                    slot.Enabled = slot.ParseTriggerKey() != FormsKeys.None;

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
            slot.TriggerKey = FormsKeys.None.ToString();
            slot.Ctrl = false;
            slot.Alt = false;
            slot.Shift = false;
            slot.Win = false;
            slot.Enabled = false;

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
            bool shouldEnable = ((CheckBox)sender).Checked && slot.ParseTriggerKey() != FormsKeys.None;
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
            FormsKeys key = slot.ParseTriggerKey();
            if (key == FormsKeys.None)
            {
                return "Not bound";
            }

            List<string> parts = new List<string>(5);
            if (slot.Ctrl) parts.Add("Ctrl");
            if (slot.Alt) parts.Add("Alt");
            if (slot.Shift) parts.Add("Shift");
            if (slot.Win) parts.Add("Win");
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
            private readonly Label lblPrompt;
            public AhkSlotConfig CapturedBinding { get; private set; }

            public BindingCaptureDialog(AhkSlotConfig currentBinding)
            {
                this.CapturedBinding = new AhkSlotConfig
                {
                    TriggerKey = currentBinding.TriggerKey,
                    Ctrl = currentBinding.Ctrl,
                    Alt = currentBinding.Alt,
                    Shift = currentBinding.Shift,
                    Win = currentBinding.Win
                };

                this.Text = "Bind Key";
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ClientSize = new Size(360, 110);
                this.KeyPreview = true;
                this.ShowInTaskbar = false;

                this.lblPrompt = new Label
                {
                    AutoSize = false,
                    Location = new Point(12, 12),
                    Size = new Size(336, 42),
                    Text = "Press the shortcut to bind.\r\nUse Ctrl, Alt, and Shift with any key. Esc cancels."
                };

                Button btnClear = new Button
                {
                    DialogResult = DialogResult.Abort,
                    Location = new Point(108, 70),
                    Size = new Size(70, 24),
                    Text = "Clear"
                };

                Button btnCancel = new Button
                {
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(188, 70),
                    Size = new Size(70, 24),
                    Text = "Cancel"
                };

                this.Controls.Add(this.lblPrompt);
                this.Controls.Add(btnClear);
                this.Controls.Add(btnCancel);
                this.AcceptButton = null;
                this.CancelButton = btnCancel;
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                base.OnKeyDown(e);

                FormsKeys keyCode = e.KeyCode & FormsKeys.KeyCode;
                if (keyCode == FormsKeys.Escape)
                {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                    return;
                }

                if (IsModifierOnlyKey(keyCode))
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                this.CapturedBinding = new AhkSlotConfig
                {
                    TriggerKey = keyCode.ToString(),
                    Ctrl = e.Control,
                    Alt = e.Alt,
                    Shift = e.Shift,
                    Win = IsWinCurrentlyPressed(),
                    ClickActive = true,
                    Enabled = true
                };

                this.DialogResult = DialogResult.OK;
                e.SuppressKeyPress = true;
                this.Close();
            }
        }
    }
}
