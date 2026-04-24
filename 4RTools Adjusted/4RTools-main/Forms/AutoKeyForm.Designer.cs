namespace _4RTools.Forms
{
    partial class AutoKeyForm
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
            this.grpKey = new System.Windows.Forms.GroupBox();
            this.lblKey = new System.Windows.Forms.Label();
            this.txtAutoKey = new System.Windows.Forms.TextBox();
            this.chkUseChord = new System.Windows.Forms.CheckBox();
            this.chkCtrl = new System.Windows.Forms.CheckBox();
            this.chkAlt = new System.Windows.Forms.CheckBox();
            this.chkShift = new System.Windows.Forms.CheckBox();
            this.chkWin = new System.Windows.Forms.CheckBox();

            this.grpInterval = new System.Windows.Forms.GroupBox();
            this.lblMinutes = new System.Windows.Forms.Label();
            this.nudMinutes = new System.Windows.Forms.NumericUpDown();
            this.lblMinUnit = new System.Windows.Forms.Label();
            this.lblSeconds = new System.Windows.Forms.Label();
            this.nudSeconds = new System.Windows.Forms.NumericUpDown();
            this.lblSecUnit = new System.Windows.Forms.Label();

            this.lblHint = new System.Windows.Forms.Label();

            ((System.ComponentModel.ISupportInitialize)(this.nudMinutes)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeconds)).BeginInit();
            this.grpKey.SuspendLayout();
            this.grpInterval.SuspendLayout();
            this.SuspendLayout();

            //
            // lblKey
            //
            this.lblKey.AutoSize = true;
            this.lblKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.lblKey.Location = new System.Drawing.Point(15, 28);
            this.lblKey.Name = "lblKey";
            this.lblKey.Size = new System.Drawing.Size(27, 15);
            this.lblKey.TabIndex = 0;
            this.lblKey.Text = "Key";

            //
            // txtAutoKey
            //
            this.txtAutoKey.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.txtAutoKey.Location = new System.Drawing.Point(60, 24);
            this.txtAutoKey.Name = "txtAutoKey";
            this.txtAutoKey.Size = new System.Drawing.Size(80, 23);
            this.txtAutoKey.TabIndex = 1;

            //
            // chkUseChord
            //
            this.chkUseChord.AutoSize = true;
            this.chkUseChord.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.chkUseChord.Location = new System.Drawing.Point(155, 27);
            this.chkUseChord.Name = "chkUseChord";
            this.chkUseChord.Size = new System.Drawing.Size(80, 19);
            this.chkUseChord.TabIndex = 2;
            this.chkUseChord.Text = "Use chord";
            this.chkUseChord.UseVisualStyleBackColor = true;

            //
            // chkCtrl
            //
            this.chkCtrl.AutoSize = true;
            this.chkCtrl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.chkCtrl.Location = new System.Drawing.Point(60, 56);
            this.chkCtrl.Name = "chkCtrl";
            this.chkCtrl.Size = new System.Drawing.Size(45, 19);
            this.chkCtrl.TabIndex = 3;
            this.chkCtrl.Text = "Ctrl";
            this.chkCtrl.UseVisualStyleBackColor = true;

            //
            // chkAlt
            //
            this.chkAlt.AutoSize = true;
            this.chkAlt.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.chkAlt.Location = new System.Drawing.Point(115, 56);
            this.chkAlt.Name = "chkAlt";
            this.chkAlt.Size = new System.Drawing.Size(40, 19);
            this.chkAlt.TabIndex = 4;
            this.chkAlt.Text = "Alt";
            this.chkAlt.UseVisualStyleBackColor = true;

            //
            // chkShift
            //
            this.chkShift.AutoSize = true;
            this.chkShift.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.chkShift.Location = new System.Drawing.Point(160, 56);
            this.chkShift.Name = "chkShift";
            this.chkShift.Size = new System.Drawing.Size(50, 19);
            this.chkShift.TabIndex = 5;
            this.chkShift.Text = "Shift";
            this.chkShift.UseVisualStyleBackColor = true;

            //
            // chkWin
            //
            this.chkWin.AutoSize = true;
            this.chkWin.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.chkWin.Location = new System.Drawing.Point(215, 56);
            this.chkWin.Name = "chkWin";
            this.chkWin.Size = new System.Drawing.Size(45, 19);
            this.chkWin.TabIndex = 6;
            this.chkWin.Text = "Win";
            this.chkWin.UseVisualStyleBackColor = true;

            //
            // grpKey
            //
            this.grpKey.Controls.Add(this.lblKey);
            this.grpKey.Controls.Add(this.txtAutoKey);
            this.grpKey.Controls.Add(this.chkUseChord);
            this.grpKey.Controls.Add(this.chkCtrl);
            this.grpKey.Controls.Add(this.chkAlt);
            this.grpKey.Controls.Add(this.chkShift);
            this.grpKey.Controls.Add(this.chkWin);
            this.grpKey.Location = new System.Drawing.Point(15, 12);
            this.grpKey.Name = "grpKey";
            this.grpKey.Size = new System.Drawing.Size(285, 90);
            this.grpKey.TabIndex = 0;
            this.grpKey.TabStop = false;
            this.grpKey.Text = "Key";

            //
            // lblMinutes
            //
            this.lblMinutes.AutoSize = true;
            this.lblMinutes.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.lblMinutes.Location = new System.Drawing.Point(15, 28);
            this.lblMinutes.Name = "lblMinutes";
            this.lblMinutes.Size = new System.Drawing.Size(40, 15);
            this.lblMinutes.TabIndex = 0;
            this.lblMinutes.Text = "Every";

            //
            // nudMinutes
            //
            this.nudMinutes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.nudMinutes.Location = new System.Drawing.Point(60, 24);
            this.nudMinutes.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudMinutes.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudMinutes.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudMinutes.Name = "nudMinutes";
            this.nudMinutes.Size = new System.Drawing.Size(50, 23);
            this.nudMinutes.TabIndex = 1;

            //
            // lblMinUnit
            //
            this.lblMinUnit.AutoSize = true;
            this.lblMinUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.lblMinUnit.Location = new System.Drawing.Point(112, 28);
            this.lblMinUnit.Name = "lblMinUnit";
            this.lblMinUnit.Size = new System.Drawing.Size(28, 15);
            this.lblMinUnit.TabIndex = 2;
            this.lblMinUnit.Text = "min";

            //
            // lblSeconds
            //
            this.lblSeconds.AutoSize = true;
            this.lblSeconds.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.lblSeconds.Location = new System.Drawing.Point(150, 28);
            this.lblSeconds.Name = "lblSeconds";
            this.lblSeconds.Size = new System.Drawing.Size(10, 15);
            this.lblSeconds.TabIndex = 3;
            this.lblSeconds.Text = "+";

            //
            // nudSeconds
            //
            this.nudSeconds.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.nudSeconds.Location = new System.Drawing.Point(165, 24);
            this.nudSeconds.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudSeconds.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            this.nudSeconds.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudSeconds.Name = "nudSeconds";
            this.nudSeconds.Size = new System.Drawing.Size(50, 23);
            this.nudSeconds.TabIndex = 4;

            //
            // lblSecUnit
            //
            this.lblSecUnit.AutoSize = true;
            this.lblSecUnit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.lblSecUnit.Location = new System.Drawing.Point(217, 28);
            this.lblSecUnit.Name = "lblSecUnit";
            this.lblSecUnit.Size = new System.Drawing.Size(26, 15);
            this.lblSecUnit.TabIndex = 5;
            this.lblSecUnit.Text = "sec";

            //
            // grpInterval
            //
            this.grpInterval.Controls.Add(this.lblMinutes);
            this.grpInterval.Controls.Add(this.nudMinutes);
            this.grpInterval.Controls.Add(this.lblMinUnit);
            this.grpInterval.Controls.Add(this.lblSeconds);
            this.grpInterval.Controls.Add(this.nudSeconds);
            this.grpInterval.Controls.Add(this.lblSecUnit);
            this.grpInterval.Location = new System.Drawing.Point(15, 110);
            this.grpInterval.Name = "grpInterval";
            this.grpInterval.Size = new System.Drawing.Size(285, 60);
            this.grpInterval.TabIndex = 1;
            this.grpInterval.TabStop = false;
            this.grpInterval.Text = "Interval";

            //
            // lblHint
            //
            this.lblHint.AutoSize = true;
            this.lblHint.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Italic);
            this.lblHint.ForeColor = System.Drawing.Color.DimGray;
            this.lblHint.Location = new System.Drawing.Point(15, 178);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(0, 14);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "Sends to the same RO window as the Skill Spammer. Click Key field, then press a key to capture.";

            //
            // AutoKeyForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(560, 270);
            this.Controls.Add(this.grpKey);
            this.Controls.Add(this.grpInterval);
            this.Controls.Add(this.lblHint);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AutoKeyForm";
            this.Text = "AutoKeyForm";
            this.grpKey.ResumeLayout(false);
            this.grpKey.PerformLayout();
            this.grpInterval.ResumeLayout(false);
            this.grpInterval.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinutes)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeconds)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.GroupBox grpKey;
        private System.Windows.Forms.Label lblKey;
        private System.Windows.Forms.TextBox txtAutoKey;
        private System.Windows.Forms.CheckBox chkUseChord;
        private System.Windows.Forms.CheckBox chkCtrl;
        private System.Windows.Forms.CheckBox chkAlt;
        private System.Windows.Forms.CheckBox chkShift;
        private System.Windows.Forms.CheckBox chkWin;
        private System.Windows.Forms.GroupBox grpInterval;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.NumericUpDown nudMinutes;
        private System.Windows.Forms.Label lblMinUnit;
        private System.Windows.Forms.Label lblSeconds;
        private System.Windows.Forms.NumericUpDown nudSeconds;
        private System.Windows.Forms.Label lblSecUnit;
        private System.Windows.Forms.Label lblHint;
    }
}
