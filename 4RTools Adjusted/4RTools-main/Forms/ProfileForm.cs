using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class ProfileForm : Form
    {
        private Container container;
        private const string PlaceholderText = "Enter profile name...";

        public ProfileForm(Container container)
        {
            InitializeComponent();
            this.container = container;
            RefreshAll();
        }

        // ═══════════════════════════════════════════
        //  Refresh helpers
        // ═══════════════════════════════════════════

        private void RefreshAll()
        {
            RefreshProfileList();
            RefreshActiveLabel();
        }

        private void RefreshProfileList()
        {
            this.lbProfilesList.Items.Clear();
            foreach (string profile in Profile.ListAll())
            {
                this.lbProfilesList.Items.Add(profile);
            }
        }

        private void RefreshActiveLabel()
        {
            string name = ProfileSingleton.GetCurrent()?.Name ?? "Default";
            this.lblActiveProfile.Text = name;
        }

        // ═══════════════════════════════════════════
        //  Active Profile — Load Selected
        // ═══════════════════════════════════════════

        private void btnLoadSelected_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("Select a profile from the list first.",
                    "Load Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selected = this.lbProfilesList.SelectedItem.ToString();
            this.container.LoadProfileAndNotify(selected);
            RefreshActiveLabel();
            RefreshProfileList();
        }

        // ═══════════════════════════════════════════
        //  Create New Profile
        // ═══════════════════════════════════════════

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string name = this.txtProfileName.Text.Trim();
            if (string.IsNullOrEmpty(name) || name == PlaceholderText)
            {
                return;
            }

            ProfileSingleton.Create(name);
            this.txtProfileName.Text = PlaceholderText;
            this.txtProfileName.ForeColor = Color.Gray;
            RefreshAll();
            this.container.refreshProfileList();

            // Sync Container state: update header dropdown, currentProfile, and _last_profile.txt
            this.container.LoadProfileAndNotify(name);
            RefreshActiveLabel();
        }

        // Placeholder text behavior
        private void txtProfileName_Enter(object sender, EventArgs e)
        {
            if (this.txtProfileName.Text == PlaceholderText)
            {
                this.txtProfileName.Text = "";
                this.txtProfileName.ForeColor = Color.Black;
            }
        }

        private void txtProfileName_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.txtProfileName.Text))
            {
                this.txtProfileName.Text = PlaceholderText;
                this.txtProfileName.ForeColor = Color.Gray;
            }
        }

        // ═══════════════════════════════════════════
        //  Manage — Rename
        // ═══════════════════════════════════════════

        private void btnRename_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("Select a profile from the list first.",
                    "Rename", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selected = this.lbProfilesList.SelectedItem.ToString();
            if (selected == "Default")
            {
                MessageBox.Show("The Default profile cannot be renamed.",
                    "Rename", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            EditProfileName dialog = new EditProfileName();
            dialog.SetProfileName(selected);
            dialog.ShowDialog();

            if (dialog.DialogResult == DialogResult.OK)
            {
                RefreshAll();
                this.container.refreshProfileList();
            }
        }

        // ═══════════════════════════════════════════
        //  Manage — Duplicate
        // ═══════════════════════════════════════════

        private void btnCopy_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("Select a profile from the list first.",
                    "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selected = this.lbProfilesList.SelectedItem.ToString();
            ProfileSingleton.Copy(selected);
            RefreshAll();
            this.container.refreshProfileList();
        }

        // ═══════════════════════════════════════════
        //  Manage — Delete
        // ═══════════════════════════════════════════

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("Select a profile from the list first.",
                    "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selected = this.lbProfilesList.SelectedItem.ToString();
            if (selected == "Default")
            {
                MessageBox.Show("The Default profile cannot be deleted.",
                    "Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Delete profile \"{selected}\"?\nThis cannot be undone.",
                "Delete Profile", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                bool wasActive = string.Equals(
                    ProfileSingleton.GetCurrent()?.Name,
                    selected,
                    StringComparison.Ordinal);

                ProfileSingleton.Delete(selected);
                RefreshAll();
                this.container.refreshProfileList();

                if (wasActive)
                {
                    this.container.LoadProfileAndNotify("Default");
                    RefreshActiveLabel();
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Manage — Export
        // ═══════════════════════════════════════════

        private void btnExport_Click(object sender, EventArgs e)
        {
            if (this.lbProfilesList.SelectedItem == null)
            {
                MessageBox.Show("Select a profile from the list first.",
                    "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selected = this.lbProfilesList.SelectedItem.ToString();
            string sourceFile = AppConfig.GetProfilePath(selected);

            if (!File.Exists(sourceFile))
            {
                MessageBox.Show("Profile file not found.",
                    "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = "Export Profile";
                dlg.FileName = selected + ".json";
                dlg.Filter = "JSON Profile (*.json)|*.json";
                dlg.DefaultExt = "json";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(sourceFile, dlg.FileName, true);
                        MessageBox.Show($"Profile \"{selected}\" exported successfully.",
                            "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Export failed: {ex.Message}",
                            "Export", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Manage — Import
        // ═══════════════════════════════════════════

        private void btnImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Import Profile";
                dlg.Filter = "JSON Profile (*.json)|*.json";

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                string importName = Path.GetFileNameWithoutExtension(dlg.FileName);
                string destFile = AppConfig.GetProfilePath(importName);

                if (File.Exists(destFile))
                {
                    DialogResult overwrite = MessageBox.Show(
                        $"A profile named \"{importName}\" already exists.\nOverwrite it?",
                        "Import", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    if (overwrite != DialogResult.Yes)
                    {
                        return;
                    }
                }

                try
                {
                    AppConfig.EnsureProfileDirectory();

                    File.Copy(dlg.FileName, destFile, true);
                    RefreshAll();
                    this.container.refreshProfileList();
                    MessageBox.Show($"Profile \"{importName}\" imported successfully.",
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}",
                        "Import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // ═══════════════════════════════════════════
        //  Custom list drawing — highlight active profile
        // ═══════════════════════════════════════════

        private void lbProfilesList_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string item = this.lbProfilesList.Items[e.Index].ToString();
            string activeProfile = ProfileSingleton.GetCurrent()?.Name ?? "Default";
            bool isActive = string.Equals(item, activeProfile, StringComparison.Ordinal);
            bool isSelected = (e.State & DrawItemState.Selected) != 0;

            // Background
            Color bgColor;
            if (isSelected)
                bgColor = Color.FromArgb(30, 90, 160);
            else if (isActive)
                bgColor = Color.FromArgb(230, 242, 255);
            else
                bgColor = Color.White;

            using (SolidBrush bg = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
            }

            // Text
            Color textColor = isSelected ? Color.White : Color.FromArgb(30, 30, 30);
            Font textFont = isActive
                ? new Font("Segoe UI", 8.5F, FontStyle.Bold)
                : new Font("Segoe UI", 8.5F, FontStyle.Regular);

            string displayText = isActive ? $"\u25B6 {item}" : $"   {item}";

            using (SolidBrush fg = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(displayText, textFont, fg,
                    new RectangleF(e.Bounds.X + 2, e.Bounds.Y + 1, e.Bounds.Width - 4, e.Bounds.Height));
            }

            textFont.Dispose();
            e.DrawFocusRectangle();
        }
    }
}
