namespace _4RTools.Forms
{
    partial class ProfileForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // ── Active profile indicator ──
            this.grpActive = new System.Windows.Forms.GroupBox();
            this.lblActiveProfile = new System.Windows.Forms.Label();
            this.btnLoadSelected = new System.Windows.Forms.Button();

            // ── Create section ──
            this.grpCreate = new System.Windows.Forms.GroupBox();
            this.txtProfileName = new System.Windows.Forms.TextBox();
            this.btnCreate = new System.Windows.Forms.Button();

            // ── Profile list & management ──
            this.grpManage = new System.Windows.Forms.GroupBox();
            this.lbProfilesList = new System.Windows.Forms.ListBox();
            this.btnRename = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnImport = new System.Windows.Forms.Button();

            this.SuspendLayout();
            this.grpActive.SuspendLayout();
            this.grpCreate.SuspendLayout();
            this.grpManage.SuspendLayout();

            // ════════════════════════════════════════
            //  grpActive — "Active Profile"
            // ════════════════════════════════════════
            this.grpActive.Location = new System.Drawing.Point(8, 4);
            this.grpActive.Size = new System.Drawing.Size(356, 42);
            this.grpActive.Text = "Active Profile";
            this.grpActive.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.grpActive.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular);
            this.grpActive.Controls.Add(this.lblActiveProfile);
            this.grpActive.Controls.Add(this.btnLoadSelected);

            // lblActiveProfile
            this.lblActiveProfile.Location = new System.Drawing.Point(10, 16);
            this.lblActiveProfile.Size = new System.Drawing.Size(220, 18);
            this.lblActiveProfile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblActiveProfile.ForeColor = System.Drawing.Color.FromArgb(30, 90, 160);
            this.lblActiveProfile.Text = "Default";
            this.lblActiveProfile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // btnLoadSelected
            this.btnLoadSelected.Location = new System.Drawing.Point(260, 14);
            this.btnLoadSelected.Size = new System.Drawing.Size(86, 23);
            this.btnLoadSelected.Text = "Load Selected";
            this.btnLoadSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadSelected.ForeColor = System.Drawing.Color.FromArgb(30, 90, 160);
            this.btnLoadSelected.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(30, 90, 160);
            this.btnLoadSelected.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            this.btnLoadSelected.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLoadSelected.Click += new System.EventHandler(this.btnLoadSelected_Click);

            // ════════════════════════════════════════
            //  grpCreate — "New Profile"
            // ════════════════════════════════════════
            this.grpCreate.Location = new System.Drawing.Point(8, 48);
            this.grpCreate.Size = new System.Drawing.Size(356, 42);
            this.grpCreate.Text = "New Profile";
            this.grpCreate.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.grpCreate.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular);
            this.grpCreate.Controls.Add(this.txtProfileName);
            this.grpCreate.Controls.Add(this.btnCreate);

            // txtProfileName
            this.txtProfileName.Location = new System.Drawing.Point(10, 16);
            this.txtProfileName.Size = new System.Drawing.Size(244, 22);
            this.txtProfileName.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.txtProfileName.ForeColor = System.Drawing.Color.Gray;
            this.txtProfileName.Text = "Enter profile name...";
            this.txtProfileName.Enter += new System.EventHandler(this.txtProfileName_Enter);
            this.txtProfileName.Leave += new System.EventHandler(this.txtProfileName_Leave);

            // btnCreate
            this.btnCreate.Location = new System.Drawing.Point(260, 15);
            this.btnCreate.Size = new System.Drawing.Size(86, 23);
            this.btnCreate.Text = "Create";
            this.btnCreate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreate.ForeColor = System.Drawing.Color.FromArgb(40, 140, 60);
            this.btnCreate.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(40, 140, 60);
            this.btnCreate.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
            this.btnCreate.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);

            // ════════════════════════════════════════
            //  grpManage — "Manage Profiles"
            // ════════════════════════════════════════
            this.grpManage.Location = new System.Drawing.Point(8, 92);
            this.grpManage.Size = new System.Drawing.Size(356, 134);
            this.grpManage.Text = "Manage Profiles";
            this.grpManage.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.grpManage.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular);
            this.grpManage.Controls.Add(this.lbProfilesList);
            this.grpManage.Controls.Add(this.btnRename);
            this.grpManage.Controls.Add(this.btnCopy);
            this.grpManage.Controls.Add(this.btnDelete);
            this.grpManage.Controls.Add(this.btnExport);
            this.grpManage.Controls.Add(this.btnImport);

            // lbProfilesList
            this.lbProfilesList.Location = new System.Drawing.Point(10, 18);
            this.lbProfilesList.Size = new System.Drawing.Size(244, 108);
            this.lbProfilesList.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.lbProfilesList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lbProfilesList.IntegralHeight = false;
            this.lbProfilesList.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lbProfilesList.ItemHeight = 20;
            this.lbProfilesList.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lbProfilesList_DrawItem);

            int btnW = 86, btnH = 23, btnX = 260, gap = 3;
            System.Drawing.Font btnFont = new System.Drawing.Font("Segoe UI", 7.5F);

            // btnRename
            this.btnRename.Location = new System.Drawing.Point(btnX, 18);
            this.btnRename.Size = new System.Drawing.Size(btnW, btnH);
            this.btnRename.Text = "Rename";
            this.btnRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRename.Font = btnFont;
            this.btnRename.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.btnRename.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.btnRename.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRename.Click += new System.EventHandler(this.btnRename_Click);

            // btnCopy
            this.btnCopy.Location = new System.Drawing.Point(btnX, 18 + (btnH + gap));
            this.btnCopy.Size = new System.Drawing.Size(btnW, btnH);
            this.btnCopy.Text = "Duplicate";
            this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopy.Font = btnFont;
            this.btnCopy.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.btnCopy.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.btnCopy.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);

            // btnDelete
            this.btnDelete.Location = new System.Drawing.Point(btnX, 18 + 2 * (btnH + gap));
            this.btnDelete.Size = new System.Drawing.Size(btnW, btnH);
            this.btnDelete.Text = "Delete";
            this.btnDelete.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDelete.Font = btnFont;
            this.btnDelete.ForeColor = System.Drawing.Color.FromArgb(180, 50, 50);
            this.btnDelete.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 50, 50);
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            // btnExport
            this.btnExport.Location = new System.Drawing.Point(btnX, 18 + 3 * (btnH + gap) + 4);
            this.btnExport.Size = new System.Drawing.Size(41, btnH);
            this.btnExport.Text = "Export";
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = btnFont;
            this.btnExport.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.btnExport.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            // btnImport
            this.btnImport.Location = new System.Drawing.Point(btnX + 44, 18 + 3 * (btnH + gap) + 4);
            this.btnImport.Size = new System.Drawing.Size(42, btnH);
            this.btnImport.Text = "Import";
            this.btnImport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImport.Font = btnFont;
            this.btnImport.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100);
            this.btnImport.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(180, 180, 180);
            this.btnImport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);

            // ════════════════════════════════════════
            //  ProfileForm
            // ════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(374, 232);
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.grpActive);
            this.Controls.Add(this.grpCreate);
            this.Controls.Add(this.grpManage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ProfileForm";
            this.Text = "ProfileForm";

            this.grpActive.ResumeLayout(false);
            this.grpCreate.ResumeLayout(false);
            this.grpCreate.PerformLayout();
            this.grpManage.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpActive;
        private System.Windows.Forms.Label lblActiveProfile;
        private System.Windows.Forms.Button btnLoadSelected;

        private System.Windows.Forms.GroupBox grpCreate;
        private System.Windows.Forms.TextBox txtProfileName;
        private System.Windows.Forms.Button btnCreate;

        private System.Windows.Forms.GroupBox grpManage;
        private System.Windows.Forms.ListBox lbProfilesList;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnImport;
    }
}
