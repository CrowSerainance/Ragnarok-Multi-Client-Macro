namespace _4RTools.Forms
{
    partial class AHKForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelSlotsHost = new System.Windows.Forms.Panel();
            this.panelSlots = new System.Windows.Forms.Panel();
            this.lblBindingHint = new System.Windows.Forms.Label();
            this.groupAhkConfig = new System.Windows.Forms.GroupBox();
            this.chkNoShift = new System.Windows.Forms.CheckBox();
            this.chkMouseFlick = new System.Windows.Forms.CheckBox();
            this.ahkSpeedBoost = new System.Windows.Forms.RadioButton();
            this.ahkCompatibility = new System.Windows.Forms.RadioButton();
            this.groupBoxDelay = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtSpammerDelay = new System.Windows.Forms.NumericUpDown();
            this.panelSlotsHost.SuspendLayout();
            this.groupAhkConfig.SuspendLayout();
            this.groupBoxDelay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpammerDelay)).BeginInit();
            this.SuspendLayout();
            // 
            // panelSlotsHost
            // 
            this.panelSlotsHost.AutoScroll = true;
            this.panelSlotsHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelSlotsHost.Controls.Add(this.panelSlots);
            this.panelSlotsHost.Location = new System.Drawing.Point(12, 12);
            this.panelSlotsHost.Name = "panelSlotsHost";
            this.panelSlotsHost.Size = new System.Drawing.Size(536, 142);
            this.panelSlotsHost.TabIndex = 0;
            // 
            // panelSlots
            // 
            this.panelSlots.Location = new System.Drawing.Point(0, 0);
            this.panelSlots.Name = "panelSlots";
            this.panelSlots.Size = new System.Drawing.Size(510, 310);
            this.panelSlots.TabIndex = 0;
            // 
            // lblBindingHint
            // 
            this.lblBindingHint.AutoSize = true;
            this.lblBindingHint.Location = new System.Drawing.Point(12, 160);
            this.lblBindingHint.Name = "lblBindingHint";
            this.lblBindingHint.Size = new System.Drawing.Size(514, 13);
            this.lblBindingHint.TabIndex = 1;
            this.lblBindingHint.Text = "Bind keyboard shortcuts here. For mouse side buttons beyond standard Windows keys" +
    ", remap them to keyboard keys in Synapse first.";
            // 
            // groupAhkConfig
            // 
            this.groupAhkConfig.Controls.Add(this.chkNoShift);
            this.groupAhkConfig.Controls.Add(this.chkMouseFlick);
            this.groupAhkConfig.Controls.Add(this.ahkSpeedBoost);
            this.groupAhkConfig.Controls.Add(this.ahkCompatibility);
            this.groupAhkConfig.Controls.Add(this.groupBoxDelay);
            this.groupAhkConfig.Location = new System.Drawing.Point(12, 179);
            this.groupAhkConfig.Name = "groupAhkConfig";
            this.groupAhkConfig.Size = new System.Drawing.Size(536, 79);
            this.groupAhkConfig.TabIndex = 2;
            this.groupAhkConfig.TabStop = false;
            this.groupAhkConfig.Text = "AHK Configuration";
            // 
            // chkNoShift
            // 
            this.chkNoShift.AutoSize = true;
            this.chkNoShift.Location = new System.Drawing.Point(155, 48);
            this.chkNoShift.Name = "chkNoShift";
            this.chkNoShift.Size = new System.Drawing.Size(64, 17);
            this.chkNoShift.TabIndex = 4;
            this.chkNoShift.Text = "No Shift";
            this.chkNoShift.UseVisualStyleBackColor = true;
            this.chkNoShift.CheckedChanged += new System.EventHandler(this.chkNoShift_CheckedChanged);
            // 
            // chkMouseFlick
            // 
            this.chkMouseFlick.AutoSize = true;
            this.chkMouseFlick.Location = new System.Drawing.Point(155, 25);
            this.chkMouseFlick.Name = "chkMouseFlick";
            this.chkMouseFlick.Size = new System.Drawing.Size(83, 17);
            this.chkMouseFlick.TabIndex = 3;
            this.chkMouseFlick.Text = "Mouse Flick";
            this.chkMouseFlick.UseVisualStyleBackColor = true;
            this.chkMouseFlick.CheckedChanged += new System.EventHandler(this.chkMouseFlick_CheckedChanged);
            // 
            // ahkSpeedBoost
            // 
            this.ahkSpeedBoost.AutoSize = true;
            this.ahkSpeedBoost.Location = new System.Drawing.Point(17, 48);
            this.ahkSpeedBoost.Name = "ahkSpeedBoost";
            this.ahkSpeedBoost.Size = new System.Drawing.Size(85, 17);
            this.ahkSpeedBoost.TabIndex = 2;
            this.ahkSpeedBoost.TabStop = true;
            this.ahkSpeedBoost.Text = "Speed boost";
            this.ahkSpeedBoost.UseVisualStyleBackColor = true;
            this.ahkSpeedBoost.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // ahkCompatibility
            // 
            this.ahkCompatibility.AutoSize = true;
            this.ahkCompatibility.Location = new System.Drawing.Point(17, 25);
            this.ahkCompatibility.Name = "ahkCompatibility";
            this.ahkCompatibility.Size = new System.Drawing.Size(83, 17);
            this.ahkCompatibility.TabIndex = 1;
            this.ahkCompatibility.TabStop = true;
            this.ahkCompatibility.Text = "Compatibility";
            this.ahkCompatibility.UseVisualStyleBackColor = true;
            this.ahkCompatibility.CheckedChanged += new System.EventHandler(this.RadioButton_CheckedChanged);
            // 
            // groupBoxDelay
            // 
            this.groupBoxDelay.Controls.Add(this.label1);
            this.groupBoxDelay.Controls.Add(this.txtSpammerDelay);
            this.groupBoxDelay.Location = new System.Drawing.Point(390, 15);
            this.groupBoxDelay.Name = "groupBoxDelay";
            this.groupBoxDelay.Size = new System.Drawing.Size(128, 52);
            this.groupBoxDelay.TabIndex = 0;
            this.groupBoxDelay.TabStop = false;
            this.groupBoxDelay.Text = "Spammer Delay";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(78, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "ms";
            // 
            // txtSpammerDelay
            // 
            this.txtSpammerDelay.Location = new System.Drawing.Point(12, 19);
            this.txtSpammerDelay.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.txtSpammerDelay.Name = "txtSpammerDelay";
            this.txtSpammerDelay.Size = new System.Drawing.Size(60, 20);
            this.txtSpammerDelay.TabIndex = 0;
            this.txtSpammerDelay.ValueChanged += new System.EventHandler(this.txtSpammerDelay_TextChanged);
            // 
            // AHKForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(560, 270);
            this.Controls.Add(this.groupAhkConfig);
            this.Controls.Add(this.lblBindingHint);
            this.Controls.Add(this.panelSlotsHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AHKForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "AHKForm";
            this.panelSlotsHost.ResumeLayout(false);
            this.groupAhkConfig.ResumeLayout(false);
            this.groupAhkConfig.PerformLayout();
            this.groupBoxDelay.ResumeLayout(false);
            this.groupBoxDelay.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtSpammerDelay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelSlotsHost;
        private System.Windows.Forms.Panel panelSlots;
        private System.Windows.Forms.Label lblBindingHint;
        private System.Windows.Forms.GroupBox groupAhkConfig;
        private System.Windows.Forms.CheckBox chkNoShift;
        private System.Windows.Forms.CheckBox chkMouseFlick;
        private System.Windows.Forms.RadioButton ahkSpeedBoost;
        private System.Windows.Forms.RadioButton ahkCompatibility;
        private System.Windows.Forms.GroupBox groupBoxDelay;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown txtSpammerDelay;
    }
}
