namespace BlackBerry.Package.Dialogs
{
    partial class DeviceForm
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
            if (disposing)
            {
                if (_runner != null)
                {
                    _runner.Dispose();
                    _runner = null;
                }
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttSetName = new System.Windows.Forms.Button();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtIP = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbType = new System.Windows.Forms.ComboBox();
            this.cmbNames = new System.Windows.Forms.ComboBox();
            this.txtLogs = new System.Windows.Forms.TextBox();
            this.bttTest = new System.Windows.Forms.Button();
            this.bttOK = new System.Windows.Forms.Button();
            this.bttCancel = new System.Windows.Forms.Button();
            this.bttDiscover = new System.Windows.Forms.Button();
            this.txtPIN = new System.Windows.Forms.TextBox();
            this.lblPIN = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttSetName);
            this.groupBox1.Controls.Add(this.txtPassword);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtIP);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtName);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.lblType);
            this.groupBox1.Controls.Add(this.cmbType);
            this.groupBox1.Controls.Add(this.cmbNames);
            this.groupBox1.Controls.Add(this.txtPIN);
            this.groupBox1.Controls.Add(this.lblPIN);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(404, 141);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Device Target Properties";
            // 
            // bttSetName
            // 
            this.bttSetName.Location = new System.Drawing.Point(260, 50);
            this.bttSetName.Name = "bttSetName";
            this.bttSetName.Size = new System.Drawing.Size(138, 23);
            this.bttSetName.TabIndex = 7;
            this.bttSetName.Text = "Set ";
            this.bttSetName.UseVisualStyleBackColor = true;
            this.bttSetName.Visible = false;
            this.bttSetName.Click += new System.EventHandler(this.bttSetName_Click);
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(89, 103);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.PasswordChar = '*';
            this.txtPassword.Size = new System.Drawing.Size(156, 20);
            this.txtPassword.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 106);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Password:";
            // 
            // txtIP
            // 
            this.txtIP.Location = new System.Drawing.Point(89, 76);
            this.txtIP.Name = "txtIP";
            this.txtIP.Size = new System.Drawing.Size(126, 20);
            this.txtIP.TabIndex = 9;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 79);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(20, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "IP:";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(89, 50);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(156, 20);
            this.txtName.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Name:";
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(17, 26);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(34, 13);
            this.lblType.TabIndex = 0;
            this.lblType.Text = "Type:";
            // 
            // cmbType
            // 
            this.cmbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbType.FormattingEnabled = true;
            this.cmbType.Items.AddRange(new object[] {
            "WiFi device",
            "USB device",
            "simulator"});
            this.cmbType.Location = new System.Drawing.Point(89, 23);
            this.cmbType.Name = "cmbType";
            this.cmbType.Size = new System.Drawing.Size(126, 21);
            this.cmbType.TabIndex = 2;
            this.cmbType.SelectedIndexChanged += new System.EventHandler(this.cmbType_SelectedIndexChanged);
            // 
            // cmbNames
            // 
            this.cmbNames.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbNames.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNames.FormattingEnabled = true;
            this.cmbNames.Location = new System.Drawing.Point(89, 50);
            this.cmbNames.MaxDropDownItems = 25;
            this.cmbNames.Name = "cmbNames";
            this.cmbNames.Size = new System.Drawing.Size(156, 21);
            this.cmbNames.TabIndex = 5;
            this.cmbNames.Visible = false;
            this.cmbNames.SelectedIndexChanged += new System.EventHandler(this.cmbNames_SelectedIndexChanged);
            // 
            // txtLogs
            // 
            this.txtLogs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogs.Location = new System.Drawing.Point(12, 159);
            this.txtLogs.Multiline = true;
            this.txtLogs.Name = "txtLogs";
            this.txtLogs.ReadOnly = true;
            this.txtLogs.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogs.Size = new System.Drawing.Size(404, 85);
            this.txtLogs.TabIndex = 1;
            // 
            // bttTest
            // 
            this.bttTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttTest.Location = new System.Drawing.Point(12, 250);
            this.bttTest.Name = "bttTest";
            this.bttTest.Size = new System.Drawing.Size(75, 23);
            this.bttTest.TabIndex = 3;
            this.bttTest.Text = "&Test";
            this.bttTest.UseVisualStyleBackColor = true;
            this.bttTest.Click += new System.EventHandler(this.bttTest_Click);
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.Location = new System.Drawing.Point(260, 250);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 4;
            this.bttOK.Text = "&OK";
            this.bttOK.UseVisualStyleBackColor = true;
            this.bttOK.Click += new System.EventHandler(this.bttOK_Click);
            // 
            // bttCancel
            // 
            this.bttCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttCancel.Location = new System.Drawing.Point(341, 250);
            this.bttCancel.Name = "bttCancel";
            this.bttCancel.Size = new System.Drawing.Size(75, 23);
            this.bttCancel.TabIndex = 5;
            this.bttCancel.Text = "&Cancel";
            this.bttCancel.UseVisualStyleBackColor = true;
            // 
            // bttDiscover
            // 
            this.bttDiscover.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttDiscover.Location = new System.Drawing.Point(10, 250);
            this.bttDiscover.Name = "bttDiscover";
            this.bttDiscover.Size = new System.Drawing.Size(75, 23);
            this.bttDiscover.TabIndex = 2;
            this.bttDiscover.Text = "&Discover";
            this.bttDiscover.UseVisualStyleBackColor = true;
            this.bttDiscover.Visible = false;
            this.bttDiscover.Click += new System.EventHandler(this.bttTest_Click);
            // 
            // txtPIN
            // 
            this.txtPIN.Location = new System.Drawing.Point(89, 23);
            this.txtPIN.Name = "txtPIN";
            this.txtPIN.ReadOnly = true;
            this.txtPIN.Size = new System.Drawing.Size(126, 20);
            this.txtPIN.TabIndex = 3;
            this.txtPIN.Visible = false;
            // 
            // lblPIN
            // 
            this.lblPIN.AutoSize = true;
            this.lblPIN.Location = new System.Drawing.Point(17, 26);
            this.lblPIN.Name = "lblPIN";
            this.lblPIN.Size = new System.Drawing.Size(28, 13);
            this.lblPIN.TabIndex = 1;
            this.lblPIN.Text = "PIN:";
            this.lblPIN.Visible = false;
            // 
            // DeviceForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttCancel;
            this.ClientSize = new System.Drawing.Size(428, 285);
            this.Controls.Add(this.bttCancel);
            this.Controls.Add(this.bttOK);
            this.Controls.Add(this.bttTest);
            this.Controls.Add(this.txtLogs);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttDiscover);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeviceForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DeviceForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtIP;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbType;
        private System.Windows.Forms.TextBox txtLogs;
        private System.Windows.Forms.Button bttTest;
        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.Button bttCancel;
        private System.Windows.Forms.Button bttSetName;
        private System.Windows.Forms.ComboBox cmbNames;
        private System.Windows.Forms.Button bttDiscover;
        private System.Windows.Forms.Label lblPIN;
        private System.Windows.Forms.TextBox txtPIN;
    }
}