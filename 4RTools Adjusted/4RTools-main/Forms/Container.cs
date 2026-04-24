using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using _4RTools.Model;
using _4RTools.Utils;

namespace _4RTools.Forms
{
    public partial class Container : Form, IObserver
    {

        private Subject subject = new Subject();
        private string currentProfile;
        public Container()
        {
            this.subject.Attach(this);

            InitializeComponent();
            this.Text = AppConfig.Name + " - " + AppConfig.Version; // Window title

            //Container Configuration
            this.IsMdiContainer = true;
            SetBackGroundColorOfMDIForm();

            //Paint Children Forms
            SetToggleApplicationStateWindow();
            SetAutopotWindow();
            SetAutopotYggWindow();
            SetSkillTimerWindow();
            SetAutoKeyWindow();
            SetProfileWindow();
            SetAHKWindow();
            SetAutobuffSkillWindow();
            SetAutobuffStuffWindow();
            SetDebuffRecoveryWindow();
            SetSongMacroWindow();
            SetATKDEFWindow();
            SetMacroSwitchWindow();
            SetServerWindow();

            TrackerSingleton.Instance().SendEvent("desktop_login", "page_view", "desktop_container_load");
        }

        public void addform(TabPage tp, Form f)
        {

            if (!tp.Controls.Contains(f))
            {
                tp.Controls.Add(f);
                f.Dock = DockStyle.Fill;
                f.Show();
                Refresh();
            }
            Refresh();
        }

        private void SetBackGroundColorOfMDIForm()
        {
            foreach (Control ctl in this.Controls)
            {
                if ((ctl) is MdiClient)
                {
                    ctl.BackColor = Color.White;
                }

            }
        }

        private void processCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Client client = new Client(this.processCB.SelectedItem.ToString());
            ClientSingleton.Instance(client);
            subject.Notify(new Utils.Message(Utils.MessageCode.PROCESS_CHANGED, null));
        }

        private static readonly string LastProfileFile = AppConfig.GetLastProfilePath();

        private void Container_Load(object sender, EventArgs e)
        {
            Console.WriteLine("[Container_Load] START");
            AppConfig.EnsureProfileDirectory();
            ProfileSingleton.Create("Default");
            this.refreshProcessList();
            this.refreshProfileList();

            // Restore last used profile, fall back to Default.
            string lastProfile = "Default";
            try
            {
                if (System.IO.File.Exists(LastProfileFile))
                    lastProfile = System.IO.File.ReadAllText(LastProfileFile).Trim();
            }
            catch { /* ignore read errors */ }

            Console.WriteLine($"[Container_Load] lastProfile from file: \"{lastProfile}\"");
            if (!this.profileCB.Items.Contains(lastProfile))
            {
                Console.WriteLine($"[Container_Load] \"{lastProfile}\" not in dropdown, falling back to Default");
                lastProfile = "Default";
            }

            // Set currentProfile FIRST so the SelectedIndexChanged handler
            // can detect the change and call Load + Notify.
            currentProfile = "Default"; // Create() already loaded Default
            this.profileCB.SelectedItem = lastProfile;
            Console.WriteLine($"[Container_Load] Set dropdown to \"{lastProfile}\", currentProfile=\"{currentProfile}\"");
            // If lastProfile != "Default", SelectedIndexChanged fires and loads it.
            // If lastProfile == "Default", it's already loaded by Create().
            // Either way, fire PROFILE_CHANGED so all forms pick up the data.
            subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
            Console.WriteLine($"[Container_Load] END — active profile: \"{ProfileSingleton.GetCurrent()?.Name}\", AHK slots configured: {ProfileSingleton.GetCurrent()?.AHK?.Slots?.Count}");
        }

        public void refreshProfileList()
        {
            this.Invoke((MethodInvoker)delegate ()
            {
                this.profileCB.Items.Clear();
            });
            foreach (string p in Profile.ListAll())
            {
                this.profileCB.Items.Add(p);
            }
        }

        /// <summary>Selects a profile by name from the dropdown (triggers load via SelectedIndexChanged).</summary>
        public void SelectProfile(string profileName)
        {
            if (this.profileCB.Items.Contains(profileName))
            {
                this.profileCB.SelectedItem = profileName;
            }
        }

        private void refreshProcessList()
        {
            this.Invoke((MethodInvoker)delegate ()
            {
                this.processCB.Items.Clear();
            });
            foreach (Process p in Process.GetProcesses())
            {
                if (p.MainWindowTitle != "" && ClientListSingleton.ExistsByProcessName(p.ProcessName))
                {
                    this.processCB.Items.Add(string.Format("{0}.exe - {1}", p.ProcessName, p.Id));
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            this.refreshProcessList();
        }

        protected override void OnClosed(EventArgs e)
        {
            ShutdownApplication();
            base.OnClosed(e);
        }

        private void ShutdownApplication()
        {
            KeyboardHook.Disable();
            subject.Notify(new Utils.Message(MessageCode.TURN_OFF, null));
            Environment.Exit(0);
        }

        private void lblLinkGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(AppConfig.GithubLink);
        }

        private void lblLinkDiscord_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(AppConfig.DiscordLink);
        }

        private void websiteLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(AppConfig.Website);
        }

        private void profileCB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = this.profileCB.Text;
            if (string.IsNullOrWhiteSpace(selected) || selected == currentProfile)
            {
                Console.WriteLine($"[profileCB_SelectedIndexChanged] SKIP — selected=\"{selected}\", currentProfile=\"{currentProfile}\"");
                return;
            }

            try
            {
                Console.WriteLine($"[profileCB_SelectedIndexChanged] Loading profile \"{selected}\" (was \"{currentProfile}\")");
                ProfileSingleton.Load(selected);
                subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                currentProfile = selected;
                Console.WriteLine($"[profileCB_SelectedIndexChanged] Loaded OK. AHK slot[0] trigger: {ProfileSingleton.GetCurrent()?.AHK?.Slots?[0]?.TriggerKey}");

                try { System.IO.File.WriteAllText(LastProfileFile, currentProfile); }
                catch { /* non-critical */ }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ProfileSingleton.Load] Error Message: {ex.Message}");
                MessageBox.Show($"Error while loading profile: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Load button (▶) — force-reloads the currently selected profile from disk.</summary>
        private void btnProfileLoad_Click(object sender, EventArgs e)
        {
            string selected = this.profileCB.Text;
            if (string.IsNullOrWhiteSpace(selected)) return;

            try
            {
                ProfileSingleton.Load(selected);
                subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));
                currentProfile = selected;

                try { System.IO.File.WriteAllText(LastProfileFile, currentProfile); }
                catch { /* non-critical */ }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Profile.Load] {ex.Message}");
                MessageBox.Show($"Failed to load profile \"{selected}\":\n{ex.Message}",
                    "Load Profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Called by ProfileForm and other code to load a specific profile by name.</summary>
        public void LoadProfileAndNotify(string profileName)
        {
            try
            {
                ProfileSingleton.Load(profileName);
                currentProfile = profileName;
                if (this.profileCB.Items.Contains(profileName))
                {
                    this.profileCB.SelectedItem = profileName;
                }

                subject.Notify(new Utils.Message(MessageCode.PROFILE_CHANGED, null));

                try { System.IO.File.WriteAllText(LastProfileFile, currentProfile); }
                catch { /* non-critical */ }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Profile.Load] {ex.Message}");
                MessageBox.Show($"Failed to load profile \"{profileName}\":\n{ex.Message}",
                    "Load Profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Save button (💾) — saves ALL current settings to the profile selected in the dropdown.</summary>
        private void btnProfileSave_Click(object sender, EventArgs e)
        {
            string selected = this.profileCB.Text;
            if (string.IsNullOrWhiteSpace(selected)) return;

            Profile current = ProfileSingleton.GetCurrent();
            if (current.Name != selected)
            {
                // Save current session under a new filename. Do NOT call Create()+Load() here:
                // Load() re-reads JSON into the singleton and wipes Skill Spammer (AHK) and other
                // in-memory edits before we write them back.
                string path = AppConfig.GetProfilePath(selected);
                AppConfig.EnsureProfileDirectory();
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, "{}");
                }

                current.Name = selected;
                currentProfile = selected;
                refreshProfileList();
                this.profileCB.SelectedItem = selected;
            }

            try
            {
                ProfileSingleton.PersistAllConfiguration();
                currentProfile = selected;

                try { System.IO.File.WriteAllText(LastProfileFile, currentProfile); }
                catch { /* non-critical */ }

                MessageBox.Show($"Profile \"{selected}\" saved.",
                    "Save Profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save profile:\n{ex.Message}",
                    "Save Profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Update(ISubject subject)
        {
            switch ((subject as Subject).Message.code)
            {
                case MessageCode.PROCESS_CHANGED:
                case MessageCode.PROFILE_CHANGED:
                    Client client = ClientSingleton.GetClient();
                    if (client != null)
                        this.characterName.Text = client.ReadCharacterName();
                    break;
                case MessageCode.TURN_OFF:
                    this.profileCB.Enabled = true;
                    this.btnProfileLoad.Enabled = true;
                    this.btnProfileSave.Enabled = true;
                    this.processCB.Enabled = true;

                    break;
                case MessageCode.TURN_ON:
                    this.profileCB.Enabled = false;
                    this.btnProfileLoad.Enabled = false;
                    this.btnProfileSave.Enabled = false;
                    this.processCB.Enabled = false;
                    this.characterName.Text = ClientSingleton.GetClient().ReadCharacterName();
                    break;
                case MessageCode.SERVER_LIST_CHANGED:
                    this.refreshProcessList();
                    break;
                case MessageCode.CLICK_ICON_TRAY:
                    this.Show();
                    this.WindowState = FormWindowState.Normal;
                    break;
                case MessageCode.SHUTDOWN_APPLICATION:
                    this.ShutdownApplication();
                    break;
            }
        }

        private void containerResize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized) { this.Hide(); }
        }

        #region Frames

        public void SetToggleApplicationStateWindow()
        {
            ToggleApplicationStateForm frm = new ToggleApplicationStateForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            this.OnOffPanel.Controls.Add(frm);
            frm.Show();
        }

        public void SetAutopotWindow()
        {
            AutopotForm frm = new AutopotForm(subject, false);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageAutopot, frm);
        }
        public void SetAutopotYggWindow()
        {
            AutopotForm frm = new AutopotForm(subject, true);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageYggAutopot, frm);
        }

        public void SetSkillTimerWindow()
        {
            SkillTimerForm frm = new SkillTimerForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabSkillTimer, frm);
        }

        public void SetAutoKeyWindow()
        {
            AutoKeyForm frm = new AutoKeyForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageAutoKey, frm);
        }

        public void SetProfileWindow()
        {
            ProfileForm frm = new ProfileForm(this);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageProfiles, frm);
        }

        public void SetServerWindow()
        {
            ServersForm frm = new ServersForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageServer, frm);
        }

        public void SetAHKWindow()
        {
            AHKForm frm = new AHKForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageSpammer, frm);
        }

        public void SetAutobuffSkillWindow()
        {
            SkillAutoBuffForm frm = new SkillAutoBuffForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabPageAutobuffSkill, frm);
            frm.Show();
        }

        public void SetAutobuffStuffWindow()
        {
            StuffAutoBuffForm frm = new StuffAutoBuffForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabPageAutobuffStuff, frm);
        }

        public void SetDebuffRecoveryWindow()
        {
            DebuffRecoveryForm frm = new DebuffRecoveryForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            frm.Show();
            addform(this.tabDebuffRecovery, frm);
        }

        public void SetSongMacroWindow()
        {
            MacroSongForm frm = new MacroSongForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabPageMacroSongs, frm);
            frm.Show();
        }

        public void SetATKDEFWindow()
        {
            ATKDEFForm frm = new ATKDEFForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.atkDef, frm);
            frm.Show();
        }

        public void SetMacroSwitchWindow()
        {
            MacroSwitchForm frm = new MacroSwitchForm(subject);
            frm.FormBorderStyle = FormBorderStyle.None;
            frm.Location = new Point(0, 65);
            frm.MdiParent = this;
            addform(this.tabMacroSwitch, frm);
            frm.Show();
        }
        #endregion
    }
}
