namespace BlackBerry.Package.Dialogs
{
    internal partial class RegistrationForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RegistrationForm));
            this.label1 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bttOK = new System.Windows.Forms.Button();
            this.txtConfirmPassword = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.bttCreateToken = new System.Windows.Forms.Button();
            this.bttCreateCertificate = new System.Windows.Forms.Button();
            this.groupBlackBerry10 = new System.Windows.Forms.GroupBox();
            this.lblTokenExpiration = new System.Windows.Forms.Label();
            this.lblRegistration = new System.Windows.Forms.Label();
            this.bttRefresh = new System.Windows.Forms.Button();
            this.txtCertName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.bttImportCertificate = new System.Windows.Forms.Button();
            this.cmbSections = new System.Windows.Forms.ComboBox();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.groupTablet = new System.Windows.Forms.GroupBox();
            this.lblTabletRegistration = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.lnkMoreInfo = new System.Windows.Forms.LinkLabel();
            this.txtCskConfirmPassword = new System.Windows.Forms.TextBox();
            this.txtCskPassword = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.txtCsjPin = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.txtPbdtPath = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.bttPbdtNavigate = new System.Windows.Forms.Button();
            this.bttRdkNavigate = new System.Windows.Forms.Button();
            this.bttCreateSigner = new System.Windows.Forms.Button();
            this.txtRdkPath = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.bttNavigate = new System.Windows.Forms.Button();
            this.groupBlackBerry10.SuspendLayout();
            this.groupTablet.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(12, 73);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(519, 33);
            this.label1.TabIndex = 1;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(12, 26);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(519, 47);
            this.label4.TabIndex = 0;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(131, 144);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(113, 20);
            this.txtPassword.TabIndex = 5;
            this.txtPassword.TextChanged += new System.EventHandler(this.txtPassword_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 147);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Password:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(131, 118);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(184, 20);
            this.txtName.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 121);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Author Name:";
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttOK.Location = new System.Drawing.Point(474, 433);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 3;
            this.bttOK.Text = "&Close";
            this.bttOK.UseVisualStyleBackColor = true;
            // 
            // txtConfirmPassword
            // 
            this.txtConfirmPassword.Location = new System.Drawing.Point(131, 170);
            this.txtConfirmPassword.Name = "txtConfirmPassword";
            this.txtConfirmPassword.PasswordChar = '*';
            this.txtConfirmPassword.Size = new System.Drawing.Size(113, 20);
            this.txtConfirmPassword.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 173);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(94, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "Confirm Password:";
            // 
            // bttCreateToken
            // 
            this.bttCreateToken.Location = new System.Drawing.Point(15, 230);
            this.bttCreateToken.Name = "bttCreateToken";
            this.bttCreateToken.Size = new System.Drawing.Size(110, 23);
            this.bttCreateToken.TabIndex = 11;
            this.bttCreateToken.Text = "Create &Token...";
            this.bttCreateToken.UseVisualStyleBackColor = true;
            this.bttCreateToken.Click += new System.EventHandler(this.bttCreateToken_Click);
            // 
            // bttCreateCertificate
            // 
            this.bttCreateCertificate.Location = new System.Drawing.Point(131, 230);
            this.bttCreateCertificate.Name = "bttCreateCertificate";
            this.bttCreateCertificate.Size = new System.Drawing.Size(113, 23);
            this.bttCreateCertificate.TabIndex = 12;
            this.bttCreateCertificate.Text = "Create C&ertificate...";
            this.bttCreateCertificate.UseVisualStyleBackColor = true;
            this.bttCreateCertificate.Click += new System.EventHandler(this.bttCreateCertificate_Click);
            // 
            // groupBlackBerry10
            // 
            this.groupBlackBerry10.Controls.Add(this.lblTokenExpiration);
            this.groupBlackBerry10.Controls.Add(this.lblRegistration);
            this.groupBlackBerry10.Controls.Add(this.bttRefresh);
            this.groupBlackBerry10.Controls.Add(this.label4);
            this.groupBlackBerry10.Controls.Add(this.txtCertName);
            this.groupBlackBerry10.Controls.Add(this.label6);
            this.groupBlackBerry10.Controls.Add(this.bttImportCertificate);
            this.groupBlackBerry10.Controls.Add(this.bttCreateCertificate);
            this.groupBlackBerry10.Controls.Add(this.label2);
            this.groupBlackBerry10.Controls.Add(this.bttCreateToken);
            this.groupBlackBerry10.Controls.Add(this.txtName);
            this.groupBlackBerry10.Controls.Add(this.label3);
            this.groupBlackBerry10.Controls.Add(this.label5);
            this.groupBlackBerry10.Controls.Add(this.txtConfirmPassword);
            this.groupBlackBerry10.Controls.Add(this.txtPassword);
            this.groupBlackBerry10.Controls.Add(this.label1);
            this.groupBlackBerry10.Location = new System.Drawing.Point(12, 12);
            this.groupBlackBerry10.Name = "groupBlackBerry10";
            this.groupBlackBerry10.Size = new System.Drawing.Size(537, 288);
            this.groupBlackBerry10.TabIndex = 0;
            this.groupBlackBerry10.TabStop = false;
            this.groupBlackBerry10.Text = "BlackBerry 10 Devices";
            this.groupBlackBerry10.Visible = false;
            // 
            // lblTokenExpiration
            // 
            this.lblTokenExpiration.AutoSize = true;
            this.lblTokenExpiration.Location = new System.Drawing.Point(247, 264);
            this.lblTokenExpiration.Name = "lblTokenExpiration";
            this.lblTokenExpiration.Size = new System.Drawing.Size(101, 13);
            this.lblTokenExpiration.TabIndex = 14;
            this.lblTokenExpiration.Text = "Token expires at: ---";
            // 
            // lblRegistration
            // 
            this.lblRegistration.AutoSize = true;
            this.lblRegistration.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblRegistration.ForeColor = System.Drawing.Color.Navy;
            this.lblRegistration.Location = new System.Drawing.Point(247, 235);
            this.lblRegistration.Name = "lblRegistration";
            this.lblRegistration.Size = new System.Drawing.Size(197, 13);
            this.lblRegistration.TabIndex = 13;
            this.lblRegistration.Text = "Registration has been completed.";
            // 
            // bttRefresh
            // 
            this.bttRefresh.Location = new System.Drawing.Point(321, 196);
            this.bttRefresh.Name = "bttRefresh";
            this.bttRefresh.Size = new System.Drawing.Size(75, 23);
            this.bttRefresh.TabIndex = 10;
            this.bttRefresh.Text = "&Refresh";
            this.bttRefresh.UseVisualStyleBackColor = true;
            this.bttRefresh.Click += new System.EventHandler(this.bttRefresh_Click);
            // 
            // txtCertName
            // 
            this.txtCertName.Location = new System.Drawing.Point(131, 196);
            this.txtCertName.Name = "txtCertName";
            this.txtCertName.ReadOnly = true;
            this.txtCertName.Size = new System.Drawing.Size(184, 20);
            this.txtCertName.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 199);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(57, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Certificate:";
            // 
            // bttImportCertificate
            // 
            this.bttImportCertificate.Location = new System.Drawing.Point(131, 259);
            this.bttImportCertificate.Name = "bttImportCertificate";
            this.bttImportCertificate.Size = new System.Drawing.Size(113, 23);
            this.bttImportCertificate.TabIndex = 12;
            this.bttImportCertificate.Text = "I&mport Certificate...";
            this.bttImportCertificate.UseVisualStyleBackColor = true;
            this.bttImportCertificate.Click += new System.EventHandler(this.bttImportCertificate_Click);
            // 
            // cmbSections
            // 
            this.cmbSections.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSections.FormattingEnabled = true;
            this.cmbSections.Items.AddRange(new object[] {
            "BlackBerry 10 Devices",
            "BlackBerry PlayBook Tablet"});
            this.cmbSections.Location = new System.Drawing.Point(20, 8);
            this.cmbSections.Name = "cmbSections";
            this.cmbSections.Size = new System.Drawing.Size(238, 21);
            this.cmbSections.TabIndex = 16;
            this.cmbSections.SelectedIndexChanged += new System.EventHandler(this.cmbSections_SelectedIndexChanged);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 306);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(537, 121);
            this.txtLog.TabIndex = 2;
            // 
            // groupTablet
            // 
            this.groupTablet.Controls.Add(this.lblTabletRegistration);
            this.groupTablet.Controls.Add(this.label13);
            this.groupTablet.Controls.Add(this.lnkMoreInfo);
            this.groupTablet.Controls.Add(this.txtCskConfirmPassword);
            this.groupTablet.Controls.Add(this.txtCskPassword);
            this.groupTablet.Controls.Add(this.label11);
            this.groupTablet.Controls.Add(this.txtCsjPin);
            this.groupTablet.Controls.Add(this.label10);
            this.groupTablet.Controls.Add(this.txtPbdtPath);
            this.groupTablet.Controls.Add(this.label9);
            this.groupTablet.Controls.Add(this.bttPbdtNavigate);
            this.groupTablet.Controls.Add(this.bttRdkNavigate);
            this.groupTablet.Controls.Add(this.bttCreateSigner);
            this.groupTablet.Controls.Add(this.txtRdkPath);
            this.groupTablet.Controls.Add(this.label8);
            this.groupTablet.Controls.Add(this.label7);
            this.groupTablet.Controls.Add(this.label12);
            this.groupTablet.Location = new System.Drawing.Point(12, 12);
            this.groupTablet.Name = "groupTablet";
            this.groupTablet.Size = new System.Drawing.Size(537, 288);
            this.groupTablet.TabIndex = 1;
            this.groupTablet.TabStop = false;
            this.groupTablet.Text = "BlackBerry Tablet";
            this.groupTablet.Visible = false;
            // 
            // lblTabletRegistration
            // 
            this.lblTabletRegistration.AutoSize = true;
            this.lblTabletRegistration.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lblTabletRegistration.ForeColor = System.Drawing.Color.Navy;
            this.lblTabletRegistration.Location = new System.Drawing.Point(134, 264);
            this.lblTabletRegistration.Name = "lblTabletRegistration";
            this.lblTabletRegistration.Size = new System.Drawing.Size(197, 13);
            this.lblTabletRegistration.TabIndex = 16;
            this.lblTabletRegistration.Text = "Registration has been completed.";
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.Location = new System.Drawing.Point(12, 73);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(520, 36);
            this.label13.TabIndex = 2;
            this.label13.Text = "To complete the registration both - BlackBerry Signer and developer certificate -" +
    " are required. Certificate will be created automatically here or at BlackBerry 1" +
    "0 devices section on left.";
            // 
            // lnkMoreInfo
            // 
            this.lnkMoreInfo.AutoSize = true;
            this.lnkMoreInfo.Location = new System.Drawing.Point(12, 54);
            this.lnkMoreInfo.Name = "lnkMoreInfo";
            this.lnkMoreInfo.Size = new System.Drawing.Size(60, 13);
            this.lnkMoreInfo.TabIndex = 1;
            this.lnkMoreInfo.TabStop = true;
            this.lnkMoreInfo.Text = "More info...";
            this.lnkMoreInfo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkMoreInfo_LinkClicked);
            // 
            // txtCskConfirmPassword
            // 
            this.txtCskConfirmPassword.Location = new System.Drawing.Point(134, 222);
            this.txtCskConfirmPassword.Name = "txtCskConfirmPassword";
            this.txtCskConfirmPassword.PasswordChar = '*';
            this.txtCskConfirmPassword.Size = new System.Drawing.Size(113, 20);
            this.txtCskConfirmPassword.TabIndex = 14;
            // 
            // txtCskPassword
            // 
            this.txtCskPassword.Location = new System.Drawing.Point(134, 196);
            this.txtCskPassword.Name = "txtCskPassword";
            this.txtCskPassword.PasswordChar = '*';
            this.txtCskPassword.Size = new System.Drawing.Size(113, 20);
            this.txtCskPassword.TabIndex = 12;
            this.txtCskPassword.TextChanged += new System.EventHandler(this.txtCskPassword_TextChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(12, 199);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(80, 13);
            this.label11.TabIndex = 11;
            this.label11.Text = "CSK Password:";
            // 
            // txtCsjPin
            // 
            this.txtCsjPin.Location = new System.Drawing.Point(134, 170);
            this.txtCsjPin.Name = "txtCsjPin";
            this.txtCsjPin.PasswordChar = '*';
            this.txtCsjPin.Size = new System.Drawing.Size(113, 20);
            this.txtCsjPin.TabIndex = 10;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(12, 173);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(50, 13);
            this.label10.TabIndex = 9;
            this.label10.Text = "CSJ PIN:";
            // 
            // txtPbdtPath
            // 
            this.txtPbdtPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPbdtPath.Location = new System.Drawing.Point(134, 144);
            this.txtPbdtPath.Name = "txtPbdtPath";
            this.txtPbdtPath.Size = new System.Drawing.Size(344, 20);
            this.txtPbdtPath.TabIndex = 7;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 147);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(86, 13);
            this.label9.TabIndex = 6;
            this.label9.Text = "PBDT CSJ Path:";
            // 
            // bttPbdtNavigate
            // 
            this.bttPbdtNavigate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttPbdtNavigate.Location = new System.Drawing.Point(484, 144);
            this.bttPbdtNavigate.Name = "bttPbdtNavigate";
            this.bttPbdtNavigate.Size = new System.Drawing.Size(38, 23);
            this.bttPbdtNavigate.TabIndex = 8;
            this.bttPbdtNavigate.Text = "...";
            this.bttPbdtNavigate.UseVisualStyleBackColor = true;
            this.bttPbdtNavigate.Click += new System.EventHandler(this.bttPbdtNavigate_Click);
            // 
            // bttRdkNavigate
            // 
            this.bttRdkNavigate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttRdkNavigate.Location = new System.Drawing.Point(484, 118);
            this.bttRdkNavigate.Name = "bttRdkNavigate";
            this.bttRdkNavigate.Size = new System.Drawing.Size(38, 23);
            this.bttRdkNavigate.TabIndex = 5;
            this.bttRdkNavigate.Text = "...";
            this.bttRdkNavigate.UseVisualStyleBackColor = true;
            this.bttRdkNavigate.Click += new System.EventHandler(this.bttRdkNavigate_Click);
            // 
            // bttCreateSigner
            // 
            this.bttCreateSigner.Location = new System.Drawing.Point(15, 259);
            this.bttCreateSigner.Name = "bttCreateSigner";
            this.bttCreateSigner.Size = new System.Drawing.Size(110, 23);
            this.bttCreateSigner.TabIndex = 15;
            this.bttCreateSigner.Text = "Create &Signer...";
            this.bttCreateSigner.UseVisualStyleBackColor = true;
            this.bttCreateSigner.Click += new System.EventHandler(this.bttCreateSigner_Click);
            // 
            // txtRdkPath
            // 
            this.txtRdkPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRdkPath.Location = new System.Drawing.Point(134, 118);
            this.txtRdkPath.Name = "txtRdkPath";
            this.txtRdkPath.Size = new System.Drawing.Size(344, 20);
            this.txtRdkPath.TabIndex = 4;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 121);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(80, 13);
            this.label8.TabIndex = 3;
            this.label8.Text = "RDK CSJ Path:";
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(12, 20);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(520, 40);
            this.label7.TabIndex = 0;
            this.label7.Text = "Specify signing properties, that were submitted at BlackBerry CodeSigning page. Y" +
    "ou should receive required RDK and PBDT files via email before.";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(12, 225);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(94, 13);
            this.label12.TabIndex = 13;
            this.label12.Text = "Confirm Password:";
            // 
            // bttNavigate
            // 
            this.bttNavigate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttNavigate.Location = new System.Drawing.Point(12, 433);
            this.bttNavigate.Name = "bttNavigate";
            this.bttNavigate.Size = new System.Drawing.Size(75, 23);
            this.bttNavigate.TabIndex = 4;
            this.bttNavigate.Text = "&Navigate...";
            this.bttNavigate.UseVisualStyleBackColor = true;
            this.bttNavigate.Click += new System.EventHandler(this.bttNavigate_Click);
            // 
            // RegistrationForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttOK;
            this.ClientSize = new System.Drawing.Size(561, 468);
            this.Controls.Add(this.cmbSections);
            this.Controls.Add(this.bttNavigate);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.bttOK);
            this.Controls.Add(this.groupTablet);
            this.Controls.Add(this.groupBlackBerry10);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RegistrationForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Developer Registration";
            this.groupBlackBerry10.ResumeLayout(false);
            this.groupBlackBerry10.PerformLayout();
            this.groupTablet.ResumeLayout(false);
            this.groupTablet.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button bttCreateCertificate;
        private System.Windows.Forms.Button bttCreateToken;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtConfirmPassword;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBlackBerry10;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtCertName;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.GroupBox groupTablet;
        private System.Windows.Forms.LinkLabel lnkMoreInfo;
        private System.Windows.Forms.TextBox txtCskConfirmPassword;
        private System.Windows.Forms.TextBox txtCskPassword;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox txtCsjPin;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox txtPbdtPath;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button bttPbdtNavigate;
        private System.Windows.Forms.Button bttRdkNavigate;
        private System.Windows.Forms.Button bttCreateSigner;
        private System.Windows.Forms.TextBox txtRdkPath;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label lblTabletRegistration;
        private System.Windows.Forms.Button bttRefresh;
        private System.Windows.Forms.Button bttImportCertificate;
        private System.Windows.Forms.Button bttNavigate;
        private System.Windows.Forms.Label lblTokenExpiration;
        private System.Windows.Forms.Label lblRegistration;
        private System.Windows.Forms.ComboBox cmbSections;
    }
}