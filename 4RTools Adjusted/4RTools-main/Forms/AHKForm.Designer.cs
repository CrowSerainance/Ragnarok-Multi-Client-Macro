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
            this.panelSlotsHost.SuspendLayout();
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
            this.lblBindingHint.Location = new System.Drawing.Point(12, 236);
            this.lblBindingHint.MaximumSize = new System.Drawing.Size(520, 0);
            this.lblBindingHint.Name = "lblBindingHint";
            this.lblBindingHint.Size = new System.Drawing.Size(518, 26);
            this.lblBindingHint.TabIndex = 1;
            this.lblBindingHint.Text = "Per slot: set trigger + keys in order. One press runs the whole sequence once (stock SendKey/Click path). Release the trigger before pressing again to repeat.";
            //
            // AHKForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(560, 268);
            this.Controls.Add(this.lblBindingHint);
            this.Controls.Add(this.panelSlotsHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "AHKForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "AHKForm";
            this.panelSlotsHost.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel panelSlotsHost;
        private System.Windows.Forms.Panel panelSlots;
        private System.Windows.Forms.Label lblBindingHint;
    }
}
