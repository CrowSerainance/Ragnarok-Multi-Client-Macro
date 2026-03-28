using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;
using _4RTools.Utils.MuhBotCore;
using FormsKeys = System.Windows.Forms.Keys;
using WpfKey = System.Windows.Input.Key;

namespace _4RTools.Forms
{
    public partial class AHKForm : Form, IObserver
    {
        private readonly List<SlotRowControls> slotRows = new List<SlotRowControls>();
        private bool updatingUi;
        private GroupBox grpClassicSpammer;
        private RadioButton rbAhkCompatibility;
        private RadioButton rbAhkSpeedBoost;
        private CheckBox chkMouseFlickGlobal;
        private CheckBox chkNoShiftGlobal;

        public AHKForm(Subject subject)
        {
            InitializeComponent();
            InitializeClassicSpammerOptions();
            InitializeSlotRows();
            subject.Attach(this);
            UpdateUI();
        }

        private void InitializeClassicSpammerOptions()
        {
            this.grpClassicSpammer = new GroupBox
            {
                Location = new Point(12, 158),
                Size = new Size(536, 72),
                Text = "Skill Spammer style (stock 4RTools)"
            };

            this.rbAhkCompatibility = new RadioButton
            {
                Name = AHK.COMPATIBILITY,
                AutoSize = true,
                Location = new Point(10, 22),
                Text = "Compatibility"
            };
            this.rbAhkSpeedBoost = new RadioButton
            {
                Name = AHK.SPEED_BOOST,
                AutoSize = true,
                Location = new Point(120, 22),
                Text = "Speed boost"
            };

            this.chkMouseFlickGlobal = new CheckBox
            {
                AutoSize = true,
                Location = new Point(10, 46),
                Text = "Mouse flick on click"
            };
            this.chkNoShiftGlobal = new CheckBox
            {
                AutoSize = true,
                Location = new Point(200, 46),
                Text = "No-shift (tap Shift around each key)"
            };

            this.rbAhkCompatibility.CheckedChanged += this.ClassicSpammerOption_Changed;
            this.rbAhkSpeedBoost.CheckedChanged += this.ClassicSpammerOption_Changed;
            this.chkMouseFlickGlobal.CheckedChanged += this.ClassicSpammerOption_Changed;
            this.chkNoShiftGlobal.CheckedChanged += this.ClassicSpammerOption_Changed;

            this.grpClassicSpammer.Controls.Add(this.rbAhkCompatibility);
            this.grpClassicSpammer.Controls.Add(this.rbAhkSpeedBoost);
            this.grpClassicSpammer.Controls.Add(this.chkMouseFlickGlobal);
            this.grpClassicSpammer.Controls.Add(this.chkNoShiftGlobal);
            this.Controls.Add(this.grpClassicSpammer);
        }

        private void ClassicSpammerOption_Changed(object sender, EventArgs e)
        {
            if (this.updatingUi)
            {
                return;
            }

            AHK ahk = ProfileSingleton.GetCurrent().AHK;
            ahk.mouseFlick = this.chkMouseFlickGlobal.Checked;
            ahk.noShift = this.chkNoShiftGlobal.Checked;
            ahk.ahkMode = this.rbAhkSpeedBoost.Checked ? AHK.SPEED_BOOST : AHK.COMPATIBILITY;
            this.ApplySpeedBoostUiRules();
            PersistAhkConfiguration();
        }

        private void ApplySpeedBoostUiRules()
        {
            bool speed = this.rbAhkSpeedBoost != null && this.rbAhkSpeedBoost.Checked;
            if (this.chkMouseFlickGlobal != null)
            {
                this.chkMouseFlickGlobal.Enabled = !speed;
            }

            if (this.chkNoShiftGlobal != null)
            {
                this.chkNoShiftGlobal.Enabled = !speed;
            }
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
                for (int i = 0; i < slotRows.Count; i++)
                {
                    AhkSlotConfig slot = ahk.Slots[i];
                    SlotRowControls row = slotRows[i];
                    row.EnabledCheckBox.Checked = slot.Enabled;
                    row.BindingTextBox.Text = FormatBinding(slot);
                }

                bool speedBoost = string.Equals(ahk.ahkMode, AHK.SPEED_BOOST, StringComparison.Ordinal);
                this.rbAhkCompatibility.Checked = !speedBoost;
                this.rbAhkSpeedBoost.Checked = speedBoost;
                this.chkMouseFlickGlobal.Checked = ahk.mouseFlick;
                this.chkNoShiftGlobal.Checked = ahk.noShift;
                this.ApplySpeedBoostUiRules();
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
                    Size = new Size(248, 20),
                    ReadOnly = true,
                    TabStop = false
                };

                Button bindButton = new Button
                {
                    Location = new Point(310, 1),
                    Size = new Size(52, 22),
                    Text = "Bind",
                    Tag = slotIndex
                };
                bindButton.Click += this.BindButton_Click;

                CheckBox enabledCheckBox = new CheckBox
                {
                    AutoSize = true,
                    Location = new Point(372, 4),
                    Text = "On",
                    Tag = slotIndex
                };
                enabledCheckBox.CheckedChanged += this.EnabledCheckBox_CheckedChanged;

                Button clearButton = new Button
                {
                    Location = new Point(430, 1),
                    Size = new Size(52, 22),
                    Text = "Clear",
                    Tag = slotIndex
                };
                clearButton.Click += this.ClearButton_Click;

                rowPanel.Controls.Add(slotLabel);
                rowPanel.Controls.Add(bindingTextBox);
                rowPanel.Controls.Add(bindButton);
                rowPanel.Controls.Add(enabledCheckBox);
                rowPanel.Controls.Add(clearButton);
                this.panelSlots.Controls.Add(rowPanel);

                this.slotRows.Add(new SlotRowControls
                {
                    BindingTextBox = bindingTextBox,
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
            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(56, 3), Size = new Size(160, 16), Text = "Binding" });
            headerPanel.Controls.Add(new Label { AutoSize = false, Location = new Point(372, 3), Size = new Size(28, 16), Text = "Use" });

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
                    List<string> conflictNotes = ResolveSkillSpammerBindingConflicts(slotIndex);
                    PersistAhkConfiguration();
                    UpdateUI();

                    if (conflictNotes.Count > 0)
                    {
                        MessageBox.Show(
                            this,
                            "Binding saved.\r\n\r\n" + string.Join("\r\n", conflictNotes),
                            "Skill Spammer",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
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
            slot.SkillBindings = new List<AhkSkillBinding>();
            slot.TriggerKey = FormsKeys.None.ToString();
            slot.SkillKey = FormsKeys.None.ToString();
            slot.Ctrl = false;
            slot.Alt = false;
            slot.Shift = false;
            slot.Win = false;
            slot.Enabled = false;
            slot.ClickActive = true;
            slot.InterSkillDelayMs = 60;
            slot.EnsureSlotModelConsistent();

            PersistAhkConfiguration();
            UpdateUI();
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

        private static string FormatBinding(AhkSlotConfig slot)
        {
            slot.EnsureSlotModelConsistent();
            List<AhkSkillBinding> bindings = slot.GetResolvedSkillBindings();
            if (slot.ParseTriggerKey() == FormsKeys.None || bindings.Count == 0)
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

            List<string> hotkeyLabels = new List<string>();
            foreach (AhkSkillBinding b in bindings)
            {
                hotkeyLabels.Add(FormatOneSkillBarHotkey(b));
            }

            return $"{FormatOneTrigger(trig)} -> {string.Join(" | ", hotkeyLabels)}";
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

            return string.Join("+", parts);
        }

        /// <summary>Display string for one RO skill-bar cell hotkey (physical key), not a skill ID.</summary>
        private static string FormatOneSkillBarHotkey(AhkSkillBinding binding)
        {
            FormsKeys key = binding.ParseKey();
            if (key == FormsKeys.None)
            {
                return "?";
            }

            List<string> parts = new List<string>(4);
            if (binding.Ctrl) parts.Add("Ctrl");
            if (binding.Alt) parts.Add("Alt");
            if (binding.Shift) parts.Add("Shift");
            parts.Add(GetFriendlyKeyName(key));

            return string.Join("+", parts);
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

        private static void PersistAhkConfiguration()
        {
            ProfileSingleton.GetCurrent().AHK.EnsureSlotsConfigured();
            ProfileSingleton.SetConfiguration(ProfileSingleton.GetCurrent().AHK);
        }

        private static List<string> ResolveSkillSpammerBindingConflicts(int preferredSlotIndex)
        {
            List<string> notes = new List<string>();
            Profile profile = ProfileSingleton.GetCurrent();
            AHK ahk = profile.AHK;
            ahk.EnsureSlotsConfigured();

            if (preferredSlotIndex < 0 || preferredSlotIndex >= ahk.Slots.Count)
            {
                return notes;
            }

            AhkSlotConfig preferredSlot = ahk.Slots[preferredSlotIndex];
            string triggerSignature = AHK.BuildTriggerSignature(preferredSlot);
            FormsKeys mainKey = preferredSlot.ParseTriggerKey();
            HashSet<FormsKeys> sequenceKeys = new HashSet<FormsKeys>(
                preferredSlot.GetResolvedSkillBindings()
                    .Select(binding => binding.ParseKey())
                    .Where(key => key != FormsKeys.None));

            if (string.IsNullOrEmpty(triggerSignature) || mainKey == FormsKeys.None)
            {
                return notes;
            }

            bool ahkChanged = false;
            for (int i = 0; i < ahk.Slots.Count; i++)
            {
                if (i == preferredSlotIndex)
                {
                    continue;
                }

                AhkSlotConfig otherSlot = ahk.Slots[i];
                if (AHK.BuildTriggerSignature(otherSlot) != triggerSignature)
                {
                    continue;
                }

                ClearSlotBinding(otherSlot);
                ahkChanged = true;
                notes.Add($"Cleared Skill Spammer slot S{i + 1:00} because it used the same trigger.");
            }

            bool songMacroChanged = ClearMacroTriggerConflicts(profile.SongMacro, mainKey, "Song Macro", notes);
            bool macroSwitchChanged = ClearMacroTriggerConflicts(profile.MacroSwitch, mainKey, "Macro Switch", notes);
            bool atkDefChanged = ClearAtkDefConflict(profile.AtkDefMode, mainKey, notes);
            bool autoRefreshChanged = ClearAutoRefreshConflicts(profile, mainKey, sequenceKeys, notes);

            if (ahkChanged)
            {
                ProfileSingleton.SetConfiguration(profile.AHK);
            }

            if (songMacroChanged)
            {
                ProfileSingleton.SetConfiguration(profile.SongMacro);
            }

            if (macroSwitchChanged)
            {
                ProfileSingleton.SetConfiguration(profile.MacroSwitch);
            }

            if (atkDefChanged)
            {
                ProfileSingleton.SetConfiguration(profile.AtkDefMode);
            }

            if (autoRefreshChanged)
            {
                ProfileSingleton.SetConfiguration(profile.AutoRefreshSpammer1);
                ProfileSingleton.SetConfiguration(profile.AutoRefreshSpammer2);
                ProfileSingleton.SetConfiguration(profile.AutoRefreshSpammer3);
            }

            return notes;
        }

        private static bool ClearMacroTriggerConflicts(Macro macro, FormsKeys mainKey, string label, List<string> notes)
        {
            if (macro?.chainConfigs == null || mainKey == FormsKeys.None)
            {
                return false;
            }

            bool changed = false;
            for (int i = 0; i < macro.chainConfigs.Count; i++)
            {
                ChainConfig chain = macro.chainConfigs[i];
                if (TryConvertWpfKeyToFormsKey(chain.trigger) != mainKey)
                {
                    continue;
                }

                chain.trigger = WpfKey.None;
                chain.infinityLoopOn = false;
                changed = true;
                notes.Add($"Cleared {label} lane {chain.id} because it used the same trigger key.");
            }

            return changed;
        }

        private static bool ClearAtkDefConflict(ATKDEFMode atkDefMode, FormsKeys mainKey, List<string> notes)
        {
            if (atkDefMode == null || mainKey == FormsKeys.None)
            {
                return false;
            }

            if (TryConvertWpfKeyToFormsKey(atkDefMode.keySpammer) != mainKey)
            {
                return false;
            }

            atkDefMode.keySpammer = WpfKey.None;
            notes.Add("Cleared ATK x DEF spammer key because it used the same trigger key.");
            return true;
        }

        private static bool ClearAutoRefreshConflicts(Profile profile, FormsKeys triggerKey, HashSet<FormsKeys> sequenceKeys, List<string> notes)
        {
            bool changed = false;

            changed |= ClearAutoRefreshConflict(profile.AutoRefreshSpammer1, triggerKey, sequenceKeys, "Skill Timer 1", notes);
            changed |= ClearAutoRefreshConflict(profile.AutoRefreshSpammer2, triggerKey, sequenceKeys, "Skill Timer 2", notes);
            changed |= ClearAutoRefreshConflict(profile.AutoRefreshSpammer3, triggerKey, sequenceKeys, "Skill Timer 3", notes);

            return changed;
        }

        private static bool ClearAutoRefreshConflict(AutoRefreshSpammer spammer, FormsKeys triggerKey, HashSet<FormsKeys> sequenceKeys, string label, List<string> notes)
        {
            if (spammer == null)
            {
                return false;
            }

            FormsKeys refreshKey = TryConvertWpfKeyToFormsKey(spammer.RefreshKey);
            if (refreshKey == FormsKeys.None)
            {
                return false;
            }

            if (refreshKey != triggerKey && !sequenceKeys.Contains(refreshKey))
            {
                return false;
            }

            spammer.RefreshKey = WpfKey.None;
            notes.Add($"Cleared {label} because it was sending a key from this Skill Spammer trigger/chain.");
            return true;
        }

        private static void ClearSlotBinding(AhkSlotConfig slot)
        {
            if (slot == null)
            {
                return;
            }

            slot.TriggerBindings = new List<AhkTriggerBinding>();
            slot.SkillKeys = new List<string>();
            slot.SkillBindings = new List<AhkSkillBinding>();
            slot.TriggerKey = FormsKeys.None.ToString();
            slot.SkillKey = FormsKeys.None.ToString();
            slot.Ctrl = false;
            slot.Alt = false;
            slot.Shift = false;
            slot.Win = false;
            slot.Enabled = false;
            slot.ClickActive = true;
            slot.InterSkillDelayMs = 60;
            slot.EnsureSlotModelConsistent();
        }

        private static FormsKeys TryConvertWpfKeyToFormsKey(WpfKey key)
        {
            if (key == WpfKey.None)
            {
                return FormsKeys.None;
            }

            return Enum.TryParse(key.ToString(), true, out FormsKeys formsKey)
                ? formsKey
                : FormsKeys.None;
        }

        private sealed class SlotRowControls
        {
            public TextBox BindingTextBox { get; set; }
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
            private readonly NumericUpDown nudDelay;
            private readonly CheckBox chkClickActive;
            private readonly Label lblStatus;
            private readonly Button btnOk;
            private AhkTriggerBinding workingTrigger;
            private readonly List<AhkSkillBinding> workingSkillBindings;
            private int workingInterSkillDelayMs;
            private ListenMode listenMode = ListenMode.None;

            public BindingCaptureDialog(AhkSlotConfig current)
            {
                current.EnsureSlotModelConsistent();

                // Full SkillBindings (including keys that do not parse) so opening Bind does not drop profile rows.
                this.workingSkillBindings = new List<AhkSkillBinding>();
                if (current.SkillBindings != null)
                {
                    HashSet<string> seenBindings = new HashSet<string>(StringComparer.Ordinal);
                    foreach (AhkSkillBinding b in current.SkillBindings)
                    {
                        AhkSkillBinding copy = new AhkSkillBinding
                        {
                            Key = string.IsNullOrWhiteSpace(b?.Key) ? FormsKeys.None.ToString() : b.Key,
                            Ctrl = b?.Ctrl ?? false,
                            Alt = b?.Alt ?? false,
                            Shift = b?.Shift ?? false
                        };

                        string signature = copy.BuildSignature();
                        if (!string.IsNullOrEmpty(signature) && !seenBindings.Add(signature))
                        {
                            continue;
                        }

                        this.workingSkillBindings.Add(copy);
                    }
                }

                this.workingTrigger = new AhkTriggerBinding
                {
                    TriggerKey = current.TriggerKey,
                    Ctrl = current.Ctrl,
                    Alt = current.Alt,
                    Shift = current.Shift,
                    Win = current.Win
                };

                this.workingInterSkillDelayMs = Math.Max(
                    AHK.InterSkillDelayMinMs,
                    Math.Min(current.InterSkillDelayMs, AHK.InterSkillDelayMaxMs));

                this.Text = "Bind Slot";
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ClientSize = new Size(392, 334);
                this.KeyPreview = true;
                this.ShowInTaskbar = false;

                Label lblTrigger = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 10),
                    Text = "Trigger"
                };

                this.txtTrigger = new TextBox
                {
                    Location = new Point(12, 30),
                    Size = new Size(258, 22),
                    ReadOnly = true,
                    TabStop = false
                };

                Button btnSetTrigger = new Button
                {
                    Location = new Point(278, 28),
                    Size = new Size(102, 24),
                    Text = "Set…"
                };
                btnSetTrigger.Click += (_, __) => this.BeginListen(ListenMode.SetTrigger);

                Label lblHotkeys = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 58),
                    Text = "Keys in chain order (one trigger press = run all)"
                };

                this.listSkills = new ListBox
                {
                    Location = new Point(12, 76),
                    Size = new Size(368, 110),
                    IntegralHeight = false
                };

                Button btnAddSkill = new Button
                {
                    Location = new Point(12, 192),
                    Size = new Size(72, 24),
                    Text = "Add…"
                };
                btnAddSkill.Click += (_, __) => this.BeginListen(ListenMode.AddSkill);

                Button btnRemove = new Button
                {
                    Location = new Point(90, 192),
                    Size = new Size(64, 24),
                    Text = "Remove"
                };
                btnRemove.Click += this.RemoveSelectedSkill;

                Button btnMoveUp = new Button
                {
                    Location = new Point(160, 192),
                    Size = new Size(52, 24),
                    Text = "Up"
                };
                btnMoveUp.Click += this.MoveSelectedSkillUp;

                Button btnMoveDown = new Button
                {
                    Location = new Point(218, 192),
                    Size = new Size(52, 24),
                    Text = "Down"
                };
                btnMoveDown.Click += this.MoveSelectedSkillDown;

                Label lblDelay = new Label
                {
                    AutoSize = true,
                    Location = new Point(12, 222),
                    Text = "Pause between keys (ms)"
                };

                this.nudDelay = new NumericUpDown
                {
                    Location = new Point(180, 220),
                    Size = new Size(52, 22),
                    Minimum = AHK.InterSkillDelayMinMs,
                    Maximum = AHK.InterSkillDelayMaxMs,
                    Value = this.workingInterSkillDelayMs,
                    Increment = 1
                };
                this.nudDelay.ValueChanged += (_, __) =>
                {
                    this.workingInterSkillDelayMs = (int)this.nudDelay.Value;
                };

                Label lblDelayHint = new Label
                {
                    AutoSize = true,
                    Location = new Point(238, 222),
                    ForeColor = System.Drawing.SystemColors.GrayText,
                    Text = $"{AHK.InterSkillDelayMinMs}–{AHK.InterSkillDelayMaxMs}"
                };

                this.chkClickActive = new CheckBox
                {
                    AutoSize = true,
                    Location = new Point(12, 248),
                    Text = "Click after each key (stock Skill Spammer)"
                };
                this.chkClickActive.Checked = current.ClickActive;

                this.lblStatus = new Label
                {
                    AutoSize = false,
                    Location = new Point(12, 272),
                    Size = new Size(368, 18),
                    ForeColor = System.Drawing.SystemColors.GrayText,
                    Text = ""
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
                this.Controls.Add(lblHotkeys);
                this.Controls.Add(this.listSkills);
                this.Controls.Add(btnAddSkill);
                this.Controls.Add(btnRemove);
                this.Controls.Add(btnMoveUp);
                this.Controls.Add(btnMoveDown);
                this.Controls.Add(lblDelay);
                this.Controls.Add(this.nudDelay);
                this.Controls.Add(lblDelayHint);
                this.Controls.Add(this.chkClickActive);
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
                List<AhkSkillBinding> distinctBindings = GetDistinctWorkingSkillBindings();
                slot.SkillBindings = distinctBindings;
                slot.SkillKeys = distinctBindings
                    .Where(b => b.ParseKey() != FormsKeys.None)
                    .Select(b => b.Key)
                    .ToList();
                slot.TriggerKey = this.workingTrigger.TriggerKey;
                slot.Ctrl = this.workingTrigger.Ctrl;
                slot.Alt = this.workingTrigger.Alt;
                slot.Shift = this.workingTrigger.Shift;
                slot.Win = this.workingTrigger.Win;
                slot.InterSkillDelayMs = this.workingInterSkillDelayMs;
                slot.ClickActive = this.chkClickActive.Checked;
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
                               slot.GetResolvedSkillBindings().Count > 0;
            }

            private void BeginListen(ListenMode mode)
            {
                this.listenMode = mode;
                this.lblStatus.Text = mode == ListenMode.AddSkill
                    ? "Press key (modifiers ok). Esc = cancel."
                    : "Press trigger. Esc = cancel.";
            }

            private void RemoveSelectedSkill(object sender, EventArgs e)
            {
                int i = this.listSkills.SelectedIndex;
                if (i >= 0 && i < this.workingSkillBindings.Count)
                {
                    this.workingSkillBindings.RemoveAt(i);
                    this.RefreshDisplays();
                }
            }

            private void MoveSelectedSkillUp(object sender, EventArgs e)
            {
                int i = this.listSkills.SelectedIndex;
                if (i > 0 && i < this.workingSkillBindings.Count)
                {
                    AhkSkillBinding temp = this.workingSkillBindings[i];
                    this.workingSkillBindings[i] = this.workingSkillBindings[i - 1];
                    this.workingSkillBindings[i - 1] = temp;
                    this.RefreshDisplays();
                    this.listSkills.SelectedIndex = i - 1;
                }
            }

            private void MoveSelectedSkillDown(object sender, EventArgs e)
            {
                int i = this.listSkills.SelectedIndex;
                if (i >= 0 && i < this.workingSkillBindings.Count - 1)
                {
                    AhkSkillBinding temp = this.workingSkillBindings[i];
                    this.workingSkillBindings[i] = this.workingSkillBindings[i + 1];
                    this.workingSkillBindings[i + 1] = temp;
                    this.RefreshDisplays();
                    this.listSkills.SelectedIndex = i + 1;
                }
            }

            private void RefreshDisplays()
            {
                FormsKeys tk = this.workingTrigger.ParseKey();
                this.txtTrigger.Text = tk == FormsKeys.None ? "(not set)" : FormatOneTrigger(this.workingTrigger);

                this.listSkills.Items.Clear();
                foreach (AhkSkillBinding b in this.workingSkillBindings)
                {
                    this.listSkills.Items.Add(FormatOneSkillBarHotkey(b));
                }

                this.listenMode = ListenMode.None;
                this.lblStatus.Text = "Each exact key combo can appear only once per cycle.";
            }

            private void OnOkClick(object sender, EventArgs e)
            {
                if (this.workingTrigger.ParseKey() == FormsKeys.None ||
                    !this.workingSkillBindings.Any(b => b.ParseKey() != FormsKeys.None))
                {
                    MessageBox.Show(
                        this,
                        "Set a trigger and at least one key.",
                        "Bind Slot",
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

                SampleModifiers(out bool ctrl, out bool alt, out bool shift, out bool win);

                if (this.listenMode == ListenMode.AddSkill)
                {
                    AhkSkillBinding candidate = new AhkSkillBinding
                    {
                        Key = keyCode.ToString(),
                        Ctrl = ctrl,
                        Alt = alt,
                        Shift = shift
                    };

                    int duplicateIndex = FindDuplicateSkillBindingIndex(candidate);
                    if (duplicateIndex >= 0)
                    {
                        this.listenMode = ListenMode.None;
                        this.listSkills.SelectedIndex = duplicateIndex;
                        this.lblStatus.Text = "That key combo is already in the chain. It will not be added twice in one cycle.";
                        e.SuppressKeyPress = true;
                        return;
                    }

                    this.workingSkillBindings.Add(candidate);
                    this.listenMode = ListenMode.None;
                    this.RefreshDisplays();
                    e.SuppressKeyPress = true;
                    return;
                }

                // SetTrigger mode
                this.workingTrigger = new AhkTriggerBinding
                {
                    TriggerKey = keyCode.ToString(),
                    Ctrl = ctrl,
                    Alt = alt,
                    Shift = shift,
                    Win = win
                };

                this.listenMode = ListenMode.None;
                this.RefreshDisplays();
                e.SuppressKeyPress = true;
            }

            private int FindDuplicateSkillBindingIndex(AhkSkillBinding candidate)
            {
                string candidateSignature = candidate.BuildSignature();
                if (string.IsNullOrEmpty(candidateSignature))
                {
                    return -1;
                }

                for (int i = 0; i < this.workingSkillBindings.Count; i++)
                {
                    if (this.workingSkillBindings[i].BuildSignature() == candidateSignature)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private List<AhkSkillBinding> GetDistinctWorkingSkillBindings()
            {
                List<AhkSkillBinding> result = new List<AhkSkillBinding>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (AhkSkillBinding binding in this.workingSkillBindings)
                {
                    if (binding == null)
                    {
                        continue;
                    }

                    string signature = binding.BuildSignature();
                    if (string.IsNullOrEmpty(signature) || !seen.Add(signature))
                    {
                        continue;
                    }

                    result.Add(new AhkSkillBinding
                    {
                        Key = binding.ParseKey().ToString(),
                        Ctrl = binding.Ctrl,
                        Alt = binding.Alt,
                        Shift = binding.Shift
                    });
                }

                return result;
            }
        }

        /// <summary>Sample physical modifier state via GetAsyncKeyState (matches runtime trigger detection).</summary>
        private static bool IsPhysicalKeyDown(FormsKeys key)
        {
            return (Native.GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private static void SampleModifiers(out bool ctrl, out bool alt, out bool shift, out bool win)
        {
            ctrl = IsPhysicalKeyDown(FormsKeys.LControlKey) || IsPhysicalKeyDown(FormsKeys.RControlKey);
            alt = IsPhysicalKeyDown(FormsKeys.LMenu) || IsPhysicalKeyDown(FormsKeys.RMenu);
            shift = IsPhysicalKeyDown(FormsKeys.LShiftKey) || IsPhysicalKeyDown(FormsKeys.RShiftKey);
            win = IsPhysicalKeyDown(FormsKeys.LWin) || IsPhysicalKeyDown(FormsKeys.RWin);
        }
    }
}
