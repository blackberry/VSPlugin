namespace RIM.VSNDK_Package.Options
{
    partial class SigningOptionControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SigningOptionControl));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttUnregister = new System.Windows.Forms.Button();
            this.bttRegister = new System.Windows.Forms.Button();
            this.lblMore = new System.Windows.Forms.LinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this.bttDeletePassword = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtCertPath = new System.Windows.Forms.TextBox();
            this.txtAuthor = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bttRefresh = new System.Windows.Forms.Button();
            this.bttBackup = new System.Windows.Forms.Button();
            this.bttRestore = new System.Windows.Forms.Button();
            this.bttChangeCert = new System.Windows.Forms.Button();
            this.bttNavigate = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttUnregister);
            this.groupBox1.Controls.Add(this.bttRegister);
            this.groupBox1.Controls.Add(this.lblMore);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(396, 103);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "BlackBerry Signing Authority";
            // 
            // bttUnregister
            // 
            this.bttUnregister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttUnregister.Location = new System.Drawing.Point(90, 70);
            this.bttUnregister.Name = "bttUnregister";
            this.bttUnregister.Size = new System.Drawing.Size(75, 23);
            this.bttUnregister.TabIndex = 3;
            this.bttUnregister.Text = "&Unregister";
            this.bttUnregister.UseVisualStyleBackColor = true;
            this.bttUnregister.Click += new System.EventHandler(this.bttUnregister_Click);
            // 
            // bttRegister
            // 
            this.bttRegister.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttRegister.Location = new System.Drawing.Point(9, 70);
            this.bttRegister.Name = "bttRegister";
            this.bttRegister.Size = new System.Drawing.Size(75, 23);
            this.bttRegister.TabIndex = 2;
            this.bttRegister.Text = "&Register";
            this.bttRegister.UseVisualStyleBackColor = true;
            this.bttRegister.Click += new System.EventHandler(this.bttRegister_Click);
            // 
            // lblMore
            // 
            this.lblMore.AutoSize = true;
            this.lblMore.Location = new System.Drawing.Point(6, 48);
            this.lblMore.Name = "lblMore";
            this.lblMore.Size = new System.Drawing.Size(60, 13);
            this.lblMore.TabIndex = 1;
            this.lblMore.TabStop = true;
            this.lblMore.Text = "More info...";
            this.lblMore.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblMore_LinkClicked);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(6, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(384, 32);
            this.label1.TabIndex = 0;
            this.label1.Text = "You must register with BlackBerry in order to sign applications and create debug " +
    "tokens for your device.";
            // 
            // bttDeletePassword
            // 
            this.bttDeletePassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttDeletePassword.Location = new System.Drawing.Point(315, 43);
            this.bttDeletePassword.Name = "bttDeletePassword";
            this.bttDeletePassword.Size = new System.Drawing.Size(75, 23);
            this.bttDeletePassword.TabIndex = 7;
            this.bttDeletePassword.Text = "Detach";
            this.bttDeletePassword.UseVisualStyleBackColor = true;
            this.bttDeletePassword.Click += new System.EventHandler(this.bttDeletePassword_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "File name:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(41, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Author:";
            // 
            // txtCertPath
            // 
            this.txtCertPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCertPath.Location = new System.Drawing.Point(90, 19);
            this.txtCertPath.Name = "txtCertPath";
            this.txtCertPath.ReadOnly = true;
            this.txtCertPath.Size = new System.Drawing.Size(138, 20);
            this.txtCertPath.TabIndex = 1;
            // 
            // txtAuthor
            // 
            this.txtAuthor.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtAuthor.Location = new System.Drawing.Point(90, 45);
            this.txtAuthor.Name = "txtAuthor";
            this.txtAuthor.ReadOnly = true;
            this.txtAuthor.Size = new System.Drawing.Size(138, 20);
            this.txtAuthor.TabIndex = 5;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.bttRefresh);
            this.groupBox2.Controls.Add(this.bttBackup);
            this.groupBox2.Controls.Add(this.bttDeletePassword);
            this.groupBox2.Controls.Add(this.bttRestore);
            this.groupBox2.Controls.Add(this.bttChangeCert);
            this.groupBox2.Controls.Add(this.bttNavigate);
            this.groupBox2.Controls.Add(this.txtAuthor);
            this.groupBox2.Controls.Add(this.txtCertPath);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Location = new System.Drawing.Point(3, 112);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(396, 183);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Developer Certificate:";
            // 
            // bttRefresh
            // 
            this.bttRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttRefresh.Location = new System.Drawing.Point(234, 43);
            this.bttRefresh.Name = "bttRefresh";
            this.bttRefresh.Size = new System.Drawing.Size(75, 23);
            this.bttRefresh.TabIndex = 6;
            this.bttRefresh.Text = "&Refresh...";
            this.bttRefresh.UseVisualStyleBackColor = true;
            this.bttRefresh.Click += new System.EventHandler(this.bttRefresh_Click);
            // 
            // bttBackup
            // 
            this.bttBackup.Location = new System.Drawing.Point(9, 144);
            this.bttBackup.Name = "bttBackup";
            this.bttBackup.Size = new System.Drawing.Size(75, 23);
            this.bttBackup.TabIndex = 9;
            this.bttBackup.Text = "&Backup...";
            this.bttBackup.UseVisualStyleBackColor = true;
            this.bttBackup.Click += new System.EventHandler(this.bttBackup_Click);
            // 
            // bttRestore
            // 
            this.bttRestore.Location = new System.Drawing.Point(90, 144);
            this.bttRestore.Name = "bttRestore";
            this.bttRestore.Size = new System.Drawing.Size(75, 23);
            this.bttRestore.TabIndex = 10;
            this.bttRestore.Text = "R&estore...";
            this.bttRestore.UseVisualStyleBackColor = true;
            this.bttRestore.Click += new System.EventHandler(this.bttRestore_Click);
            // 
            // bttChangeCert
            // 
            this.bttChangeCert.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttChangeCert.Location = new System.Drawing.Point(234, 16);
            this.bttChangeCert.Name = "bttChangeCert";
            this.bttChangeCert.Size = new System.Drawing.Size(75, 23);
            this.bttChangeCert.TabIndex = 2;
            this.bttChangeCert.Text = "&Change...";
            this.bttChangeCert.UseVisualStyleBackColor = true;
            this.bttChangeCert.Click += new System.EventHandler(this.bttChangeCert_Click);
            // 
            // bttNavigate
            // 
            this.bttNavigate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttNavigate.Location = new System.Drawing.Point(315, 16);
            this.bttNavigate.Name = "bttNavigate";
            this.bttNavigate.Size = new System.Drawing.Size(75, 23);
            this.bttNavigate.TabIndex = 3;
            this.bttNavigate.Text = "&Open...";
            this.bttNavigate.UseVisualStyleBackColor = true;
            this.bttNavigate.Click += new System.EventHandler(this.bttNavigate_Click);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(6, 72);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(384, 73);
            this.label4.TabIndex = 8;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // SigningOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "SigningOptionControl";
            this.Size = new System.Drawing.Size(402, 298);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.LinkLabel lblMore;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtAuthor;
        private System.Windows.Forms.TextBox txtCertPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttUnregister;
        private System.Windows.Forms.Button bttRegister;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button bttDeletePassword;
        private System.Windows.Forms.Button bttNavigate;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button bttBackup;
        private System.Windows.Forms.Button bttRestore;
        private System.Windows.Forms.Button bttRefresh;
        private System.Windows.Forms.Button bttChangeCert;
    }
}
