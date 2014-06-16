namespace RIM.VSNDK_Package.Options
{
    partial class GeneralOptionControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtNdkPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttOpenProfile = new System.Windows.Forms.Button();
            this.txtProfilePath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.bttToolsBrowse = new System.Windows.Forms.Button();
            this.bttNdkBrowse = new System.Windows.Forms.Button();
            this.txtToolsPath = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.chkOpenInExternal = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtNdkPath
            // 
            this.txtNdkPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtNdkPath.Location = new System.Drawing.Point(91, 23);
            this.txtNdkPath.Name = "txtNdkPath";
            this.txtNdkPath.Size = new System.Drawing.Size(429, 20);
            this.txtNdkPath.TabIndex = 1;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttOpenProfile);
            this.groupBox1.Controls.Add(this.txtProfilePath);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.bttToolsBrowse);
            this.groupBox1.Controls.Add(this.bttNdkBrowse);
            this.groupBox1.Controls.Add(this.txtToolsPath);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtNdkPath);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(616, 159);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Locations";
            // 
            // bttOpenProfile
            // 
            this.bttOpenProfile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOpenProfile.Location = new System.Drawing.Point(526, 115);
            this.bttOpenProfile.Name = "bttOpenProfile";
            this.bttOpenProfile.Size = new System.Drawing.Size(75, 23);
            this.bttOpenProfile.TabIndex = 9;
            this.bttOpenProfile.Text = "Open...";
            this.bttOpenProfile.UseVisualStyleBackColor = true;
            this.bttOpenProfile.Click += new System.EventHandler(this.bttOpenProfile_Click);
            // 
            // txtProfilePath
            // 
            this.txtProfilePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtProfilePath.Location = new System.Drawing.Point(91, 115);
            this.txtProfilePath.Name = "txtProfilePath";
            this.txtProfilePath.ReadOnly = true;
            this.txtProfilePath.Size = new System.Drawing.Size(429, 20);
            this.txtProfilePath.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 118);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(64, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Profile Path:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Tools Path:";
            // 
            // bttToolsBrowse
            // 
            this.bttToolsBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttToolsBrowse.Location = new System.Drawing.Point(526, 76);
            this.bttToolsBrowse.Name = "bttToolsBrowse";
            this.bttToolsBrowse.Size = new System.Drawing.Size(75, 23);
            this.bttToolsBrowse.TabIndex = 6;
            this.bttToolsBrowse.Text = "Browse...";
            this.bttToolsBrowse.UseVisualStyleBackColor = true;
            this.bttToolsBrowse.Click += new System.EventHandler(this.bttToolsBrowse_Click);
            // 
            // bttNdkBrowse
            // 
            this.bttNdkBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttNdkBrowse.Location = new System.Drawing.Point(526, 23);
            this.bttNdkBrowse.Name = "bttNdkBrowse";
            this.bttNdkBrowse.Size = new System.Drawing.Size(75, 23);
            this.bttNdkBrowse.TabIndex = 2;
            this.bttNdkBrowse.Text = "Browse...";
            this.bttNdkBrowse.UseVisualStyleBackColor = true;
            this.bttNdkBrowse.Click += new System.EventHandler(this.bttNdkBrowse_Click);
            // 
            // txtToolsPath
            // 
            this.txtToolsPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtToolsPath.Location = new System.Drawing.Point(91, 76);
            this.txtToolsPath.Name = "txtToolsPath";
            this.txtToolsPath.Size = new System.Drawing.Size(429, 20);
            this.txtToolsPath.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "(Visual Studio Edition)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "NDK Path:";
            // 
            // chkOpenInExternal
            // 
            this.chkOpenInExternal.AutoSize = true;
            this.chkOpenInExternal.Location = new System.Drawing.Point(18, 175);
            this.chkOpenInExternal.Name = "chkOpenInExternal";
            this.chkOpenInExternal.Size = new System.Drawing.Size(167, 17);
            this.chkOpenInExternal.TabIndex = 1;
            this.chkOpenInExternal.Text = "Open links in external browser";
            this.chkOpenInExternal.UseVisualStyleBackColor = true;
            // 
            // GeneralOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkOpenInExternal);
            this.Controls.Add(this.groupBox1);
            this.Name = "GeneralOptionControl";
            this.Size = new System.Drawing.Size(619, 217);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtNdkPath;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bttToolsBrowse;
        private System.Windows.Forms.Button bttNdkBrowse;
        private System.Windows.Forms.TextBox txtToolsPath;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtProfilePath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button bttOpenProfile;
        private System.Windows.Forms.CheckBox chkOpenInExternal;
    }
}
