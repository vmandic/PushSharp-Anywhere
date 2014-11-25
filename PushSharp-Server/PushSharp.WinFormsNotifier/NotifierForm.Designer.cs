namespace PushSharp.WinFormsNotifier
{
    partial class NotifierForm
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lstMessages = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lstErrors = new System.Windows.Forms.ListBox();
            this.btnStartPNP = new System.Windows.Forms.Button();
            this.btnStopPNP = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstMessages
            // 
            this.lstMessages.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMessages.FormattingEnabled = true;
            this.lstMessages.Location = new System.Drawing.Point(12, 67);
            this.lstMessages.Name = "lstMessages";
            this.lstMessages.Size = new System.Drawing.Size(700, 108);
            this.lstMessages.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(141, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Push Notification Messages:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 182);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(141, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Push Notification Messages:";
            // 
            // lstErrors
            // 
            this.lstErrors.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lstErrors.FormattingEnabled = true;
            this.lstErrors.Location = new System.Drawing.Point(12, 201);
            this.lstErrors.Name = "lstErrors";
            this.lstErrors.Size = new System.Drawing.Size(700, 95);
            this.lstErrors.TabIndex = 2;
            // 
            // btnStartPNP
            // 
            this.btnStartPNP.Location = new System.Drawing.Point(16, 13);
            this.btnStartPNP.Name = "btnStartPNP";
            this.btnStartPNP.Size = new System.Drawing.Size(75, 23);
            this.btnStartPNP.TabIndex = 4;
            this.btnStartPNP.Text = "Start";
            this.btnStartPNP.UseVisualStyleBackColor = true;
            this.btnStartPNP.Click += new System.EventHandler(this.btnStartPNP_Click);
            // 
            // btnStopPNP
            // 
            this.btnStopPNP.Enabled = false;
            this.btnStopPNP.Location = new System.Drawing.Point(97, 13);
            this.btnStopPNP.Name = "btnStopPNP";
            this.btnStopPNP.Size = new System.Drawing.Size(75, 23);
            this.btnStopPNP.TabIndex = 5;
            this.btnStopPNP.Text = "Stopped";
            this.btnStopPNP.UseVisualStyleBackColor = true;
            this.btnStopPNP.Click += new System.EventHandler(this.btnStopPNP_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 305);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(724, 22);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(29, 17);
            this.lblStatus.Text = "Idle.";
            // 
            // NotifierForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 327);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnStopPNP);
            this.Controls.Add(this.btnStartPNP);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lstErrors);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstMessages);
            this.MaximumSize = new System.Drawing.Size(1366, 366);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(740, 366);
            this.Name = "NotifierForm";
            this.Text = "Push Notifications Notifier";
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox lstMessages;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lstErrors;
        private System.Windows.Forms.Button btnStartPNP;
        private System.Windows.Forms.Button btnStopPNP;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
    }
}

