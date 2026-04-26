using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class AutoKeyForm : Form, IObserver
    {
        private readonly List<SlotRow> rows = new List<SlotRow>();
        private bool suppressEvents;

        public AutoKeyForm(Subject subject)
        {
            InitializeComponent();
            BuildHeader();
            BuildSlotRows();
            subject.Attach(this);
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

        private void BuildHeader()
        {
            int y = 10;
            AddHeader("Slot",       10,  y, 40);
            AddHeader("Enabled",    65,  y, 60);
            AddHeader("Chain",      140, y, 200);
            AddHeader("Step (ms)",  410, y, 65);
            AddHeader("Every",      535, y, 50);
        }

        private void AddHeader(string text, int x, int y, int w)
        {
            Label lbl = new Label
            {
                AutoSize = false,
                Text = text,
                Font = new Font("Microsoft Sans Serif", 8.5F, FontStyle.Bold),
                Location = new Point(x, y),
                Size = new Size(w, 16)
            };
            this.Controls.Add(lbl);
        }

        private void BuildSlotRows()
        {
            int yStart = 35;
            int rowHeight = 36;

            for (int i = 0; i < AutoKey.SLOT_COUNT; i++)
            {
                int y = yStart + i * rowHeight;
                int slotIndex = i;
                SlotRow row = new SlotRow();

                row.SlotLabel = new Label
                {
                    Text = $"Slot {i + 1}",
                    Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                    Location = new Point(10, y + 5),
                    AutoSize = true
                };
                this.Controls.Add(row.SlotLabel);

                row.EnabledCheck = new CheckBox
                {
                    Text = "",
                    Location = new Point(80, y + 4),
                    AutoSize = true
                };
                row.EnabledCheck.CheckedChanged += (s, e) =>
                {
                    if (suppressEvents) return;
                    AutoKeySlot slot = ProfileSingleton.GetCurrent().AutoKey.Slots[slotIndex];
                    slot.Enabled = row.EnabledCheck.Checked;
                    PersistAndRefresh();
                };
                this.Controls.Add(row.EnabledCheck);

                row.ChainLabel = new Label
                {
                    Text = "(empty)",
                    Location = new Point(140, y + 6),
                    Size = new Size(200, 20),
                    AutoEllipsis = true,
                    BorderStyle = BorderStyle.FixedSingle,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                this.Controls.Add(row.ChainLabel);

                row.EditButton = new Button
                {
                    Text = "Edit...",
                    Location = new Point(345, y + 4),
                    Size = new Size(55, 24)
                };
                row.EditButton.Click += (s, e) => OpenChainEditor(slotIndex, row);
                this.Controls.Add(row.EditButton);

                row.StepDelayNud = new NumericUpDown
                {
                    Minimum = 10,
                    Maximum = 5000,
                    Value = 150,
                    Increment = 10,
                    Location = new Point(420, y + 5),
                    Size = new Size(60, 23)
                };
                row.StepDelayNud.ValueChanged += (s, e) =>
                {
                    if (suppressEvents) return;
                    AutoKeySlot slot = ProfileSingleton.GetCurrent().AutoKey.Slots[slotIndex];
                    slot.StepDelayMs = (int)row.StepDelayNud.Value;
                    PersistAndRefresh();
                };
                this.Controls.Add(row.StepDelayNud);

                row.MinNud = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 60,
                    Value = 1,
                    Location = new Point(535, y + 5),
                    Size = new Size(45, 23)
                };
                row.MinNud.ValueChanged += (s, e) =>
                {
                    if (suppressEvents) return;
                    AutoKeySlot slot = ProfileSingleton.GetCurrent().AutoKey.Slots[slotIndex];
                    slot.IntervalMinutes = (int)row.MinNud.Value;
                    PersistAndRefresh();
                };
                this.Controls.Add(row.MinNud);

                Label lblM = new Label { Text = "m", Location = new Point(581, y + 8), AutoSize = true };
                this.Controls.Add(lblM);

                row.SecNud = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 59,
                    Value = 0,
                    Location = new Point(605, y + 5),
                    Size = new Size(45, 23)
                };
                row.SecNud.ValueChanged += (s, e) =>
                {
                    if (suppressEvents) return;
                    AutoKeySlot slot = ProfileSingleton.GetCurrent().AutoKey.Slots[slotIndex];
                    slot.IntervalSeconds = (int)row.SecNud.Value;
                    PersistAndRefresh();
                };
                this.Controls.Add(row.SecNud);

                Label lblS = new Label { Text = "s", Location = new Point(651, y + 8), AutoSize = true };
                this.Controls.Add(lblS);

                rows.Add(row);
            }
        }

        private void OpenChainEditor(int slotIndex, SlotRow row)
        {
            AutoKeySlot slot = ProfileSingleton.GetCurrent().AutoKey.Slots[slotIndex];
            using (AutoKeyChainEditor dlg = new AutoKeyChainEditor(slot.Bindings, slotIndex + 1))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    slot.Bindings = dlg.Result;
                    UpdateChainDisplay(row, slot);
                    PersistAndRefresh();
                }
            }
        }

        private void UpdateChainDisplay(SlotRow row, AutoKeySlot slot)
        {
            if (slot.Bindings == null || slot.Bindings.Count == 0)
            {
                row.ChainLabel.Text = "(empty)";
                return;
            }

            List<string> parts = new List<string>(slot.Bindings.Count);
            foreach (AutoKeyBinding b in slot.Bindings) parts.Add(b.Display());
            row.ChainLabel.Text = string.Join(", ", parts);
        }

        private void SyncFromProfile()
        {
            AutoKey ak = ProfileSingleton.GetCurrent().AutoKey;
            if (ak == null) return;
            ak.EnsureSlots();

            suppressEvents = true;
            try
            {
                for (int i = 0; i < rows.Count && i < ak.Slots.Count; i++)
                {
                    AutoKeySlot slot = ak.Slots[i];
                    SlotRow row = rows[i];

                    row.EnabledCheck.Checked = slot.Enabled;
                    row.StepDelayNud.Value = Clamp(slot.StepDelayMs, (int)row.StepDelayNud.Minimum, (int)row.StepDelayNud.Maximum);
                    row.MinNud.Value = Clamp(slot.IntervalMinutes, (int)row.MinNud.Minimum, (int)row.MinNud.Maximum);
                    row.SecNud.Value = Clamp(slot.IntervalSeconds, (int)row.SecNud.Minimum, (int)row.SecNud.Maximum);
                    UpdateChainDisplay(row, slot);
                }
            }
            finally
            {
                suppressEvents = false;
            }
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

        private void PersistAndRefresh()
        {
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AutoKey);
        }

        private void SetEditingEnabled(bool enabled)
        {
            foreach (SlotRow r in rows)
            {
                r.EnabledCheck.Enabled = enabled;
                r.EditButton.Enabled = enabled;
                r.StepDelayNud.Enabled = enabled;
                r.MinNud.Enabled = enabled;
                r.SecNud.Enabled = enabled;
            }
        }

        private class SlotRow
        {
            public Label SlotLabel;
            public CheckBox EnabledCheck;
            public Label ChainLabel;
            public Button EditButton;
            public NumericUpDown StepDelayNud;
            public NumericUpDown MinNud;
            public NumericUpDown SecNud;
        }
    }

    internal class AutoKeyChainEditor : Form
    {
        private readonly ListBox lstChain;
        private readonly TextBox txtKey;
        private readonly CheckBox chkUseChord;
        private readonly CheckBox chkCtrl, chkAlt, chkShift, chkWin;
        private readonly Button btnAdd, btnRemove, btnUp, btnDown, btnOk, btnCancel;

        public List<AutoKeyBinding> Result { get; private set; }

        public AutoKeyChainEditor(List<AutoKeyBinding> initial, int slotNumber)
        {
            Result = new List<AutoKeyBinding>();
            if (initial != null)
            {
                foreach (AutoKeyBinding b in initial)
                    if (b != null) Result.Add(b.Clone());
            }

            this.Text = $"Slot {slotNumber} — Chain Editor";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(380, 320);
            this.BackColor = Color.White;

            Label lblOrder = new Label
            {
                Text = "Chain (fires top → bottom):",
                Location = new Point(10, 10),
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8.5F, FontStyle.Bold)
            };
            this.Controls.Add(lblOrder);

            lstChain = new ListBox { Location = new Point(10, 30), Size = new Size(260, 160) };
            this.Controls.Add(lstChain);

            btnUp = new Button { Text = "↑", Location = new Point(280, 30), Size = new Size(40, 28) };
            btnUp.Click += (s, e) => MoveSelected(-1);
            this.Controls.Add(btnUp);

            btnDown = new Button { Text = "↓", Location = new Point(280, 64), Size = new Size(40, 28) };
            btnDown.Click += (s, e) => MoveSelected(+1);
            this.Controls.Add(btnDown);

            btnRemove = new Button { Text = "Remove", Location = new Point(280, 162), Size = new Size(85, 28) };
            btnRemove.Click += (s, e) => RemoveSelected();
            this.Controls.Add(btnRemove);

            GroupBox grpAdd = new GroupBox
            {
                Text = "Add binding",
                Location = new Point(10, 200),
                Size = new Size(355, 75)
            };
            this.Controls.Add(grpAdd);

            Label lblKey = new Label { Text = "Key", Location = new Point(10, 22), AutoSize = true };
            grpAdd.Controls.Add(lblKey);

            txtKey = new TextBox
            {
                Location = new Point(40, 18),
                Size = new Size(70, 23),
                Font = new Font("Microsoft Sans Serif", 10F)
            };
            txtKey.KeyDown += new System.Windows.Forms.KeyEventHandler(FormUtils.OnKeyDown);
            txtKey.KeyPress += new KeyPressEventHandler(FormUtils.OnKeyPress);
            grpAdd.Controls.Add(txtKey);

            chkUseChord = new CheckBox { Text = "Chord:", Location = new Point(120, 20), AutoSize = true };
            chkUseChord.CheckedChanged += (s, e) => RefreshChordEnabled();
            grpAdd.Controls.Add(chkUseChord);

            chkCtrl  = new CheckBox { Text = "Ctrl",  Location = new Point(120, 45), AutoSize = true, Enabled = false };
            chkAlt   = new CheckBox { Text = "Alt",   Location = new Point(170, 45), AutoSize = true, Enabled = false };
            chkShift = new CheckBox { Text = "Shift", Location = new Point(215, 45), AutoSize = true, Enabled = false };
            chkWin   = new CheckBox { Text = "Win",   Location = new Point(265, 45), AutoSize = true, Enabled = false };
            grpAdd.Controls.Add(chkCtrl);
            grpAdd.Controls.Add(chkAlt);
            grpAdd.Controls.Add(chkShift);
            grpAdd.Controls.Add(chkWin);

            btnAdd = new Button { Text = "Add", Location = new Point(305, 17), Size = new Size(45, 26) };
            btnAdd.Click += (s, e) => AddBinding();
            grpAdd.Controls.Add(btnAdd);

            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(195, 285),
                Size = new Size(80, 26),
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 285),
                Size = new Size(80, 26),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            RefreshList();
        }

        private void RefreshChordEnabled()
        {
            bool on = chkUseChord.Checked;
            chkCtrl.Enabled = on;
            chkAlt.Enabled = on;
            chkShift.Enabled = on;
            chkWin.Enabled = on;
        }

        private void AddBinding()
        {
            if (string.IsNullOrWhiteSpace(txtKey.Text))
            {
                MessageBox.Show(this, "Click the Key field then press a key to capture.", "No key", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Key parsed;
            try { parsed = (Key)Enum.Parse(typeof(Key), txtKey.Text); }
            catch
            {
                MessageBox.Show(this, $"Unsupported key: \"{txtKey.Text}\".", "Invalid key", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AutoKeyBinding b = new AutoKeyBinding
            {
                Key = parsed,
                UseChord = chkUseChord.Checked,
                Ctrl = chkCtrl.Checked,
                Alt = chkAlt.Checked,
                Shift = chkShift.Checked,
                Win = chkWin.Checked
            };
            Result.Add(b);

            txtKey.Text = "";
            chkUseChord.Checked = false;
            chkCtrl.Checked = chkAlt.Checked = chkShift.Checked = chkWin.Checked = false;
            RefreshChordEnabled();
            RefreshList();
        }

        private void RemoveSelected()
        {
            int idx = lstChain.SelectedIndex;
            if (idx < 0 || idx >= Result.Count) return;
            Result.RemoveAt(idx);
            RefreshList();
            if (Result.Count > 0)
                lstChain.SelectedIndex = Math.Min(idx, Result.Count - 1);
        }

        private void MoveSelected(int delta)
        {
            int idx = lstChain.SelectedIndex;
            if (idx < 0) return;
            int target = idx + delta;
            if (target < 0 || target >= Result.Count) return;
            AutoKeyBinding tmp = Result[idx];
            Result[idx] = Result[target];
            Result[target] = tmp;
            RefreshList();
            lstChain.SelectedIndex = target;
        }

        private void RefreshList()
        {
            int prev = lstChain.SelectedIndex;
            lstChain.BeginUpdate();
            lstChain.Items.Clear();
            for (int i = 0; i < Result.Count; i++)
                lstChain.Items.Add($"{i + 1}.  {Result[i].Display()}");
            lstChain.EndUpdate();
            if (prev >= 0 && prev < lstChain.Items.Count)
                lstChain.SelectedIndex = prev;
        }
    }
}
