namespace BlackBerry.Package.Dialogs
{
    internal partial class AboutForm
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
            this.bttOK = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureLogo = new System.Windows.Forms.PictureBox();
            this.lblVersion = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.linkBugTracker = new System.Windows.Forms.LinkLabel();
            this.linkSourceCode = new System.Windows.Forms.LinkLabel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.linkAuthor = new System.Windows.Forms.LinkLabel();
            this.linkTwitter = new System.Windows.Forms.LinkLabel();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureLogo)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttOK.Location = new System.Drawing.Point(519, 260);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 3;
            this.bttOK.Text = "&OK";
            this.bttOK.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.pictureLogo);
            this.panel1.Location = new System.Drawing.Point(-11, -12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(623, 125);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(151, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(453, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Native Plugin for Visual Studio";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureLogo
            // 
            this.pictureLogo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureLogo.BackColor = System.Drawing.Color.Transparent;
            this.pictureLogo.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureLogo.Location = new System.Drawing.Point(22, 23);
            this.pictureLogo.Name = "pictureLogo";
            this.pictureLogo.Size = new System.Drawing.Size(582, 73);
            this.pictureLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureLogo.TabIndex = 0;
            this.pictureLogo.TabStop = false;
            // 
            // lblVersion
            // 
            this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(9, 265);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(47, 13);
            this.lblVersion.TabIndex = 2;
            this.lblVersion.Text = "[version]";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(16, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(509, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "This is open-source software. We encourage you to take part in its further develo" +
    "pment and improvements.";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(16, 59);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(165, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Report bugs and feature requests";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 83);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(165, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Full source code is also available ";
            // 
            // linkBugTracker
            // 
            this.linkBugTracker.AutoSize = true;
            this.linkBugTracker.Location = new System.Drawing.Point(179, 59);
            this.linkBugTracker.Name = "linkBugTracker";
            this.linkBugTracker.Size = new System.Drawing.Size(31, 13);
            this.linkBugTracker.TabIndex = 2;
            this.linkBugTracker.TabStop = true;
            this.linkBugTracker.Text = "here.";
            this.linkBugTracker.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkBugTracker_LinkClicked);
            // 
            // linkSourceCode
            // 
            this.linkSourceCode.AutoSize = true;
            this.linkSourceCode.Location = new System.Drawing.Point(174, 83);
            this.linkSourceCode.Name = "linkSourceCode";
            this.linkSourceCode.Size = new System.Drawing.Size(31, 13);
            this.linkSourceCode.TabIndex = 4;
            this.linkSourceCode.TabStop = true;
            this.linkSourceCode.Text = "here.";
            this.linkSourceCode.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkSourceCode_LinkClicked);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.linkBugTracker);
            this.groupBox1.Controls.Add(this.linkAuthor);
            this.groupBox1.Controls.Add(this.linkTwitter);
            this.groupBox1.Controls.Add(this.linkSourceCode);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(12, 119);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(582, 135);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(251, 108);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(304, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = ", who has improved most of features and fixed gazillion of bugs.";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 108);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(89, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Special honors to";
            // 
            // linkAuthor
            // 
            this.linkAuthor.AutoSize = true;
            this.linkAuthor.Location = new System.Drawing.Point(105, 108);
            this.linkAuthor.Name = "linkAuthor";
            this.linkAuthor.Size = new System.Drawing.Size(78, 13);
            this.linkAuthor.TabIndex = 6;
            this.linkAuthor.TabStop = true;
            this.linkAuthor.Text = "Paweł Hofman";
            this.linkAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkAuthor_LinkClicked);
            // 
            // linkTwitter
            // 
            this.linkTwitter.AutoSize = true;
            this.linkTwitter.Location = new System.Drawing.Point(182, 108);
            this.linkTwitter.Name = "linkTwitter";
            this.linkTwitter.Size = new System.Drawing.Size(72, 13);
            this.linkTwitter.TabIndex = 7;
            this.linkTwitter.TabStop = true;
            this.linkTwitter.Text = "@CodeTitans";
            this.linkTwitter.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkTwitter_LinkClicked);
            // 
            // AboutForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttOK;
            this.ClientSize = new System.Drawing.Size(606, 295);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.bttOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureLogo)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PictureBox pictureLogo;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.LinkLabel linkBugTracker;
        private System.Windows.Forms.LinkLabel linkSourceCode;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.LinkLabel linkAuthor;
        private System.Windows.Forms.LinkLabel linkTwitter;
    }
}