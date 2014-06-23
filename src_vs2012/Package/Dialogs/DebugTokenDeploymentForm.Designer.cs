namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class DebugTokenDeploymentForm
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
                if (_deviceInfoRunner != null)
                {
                    _deviceInfoRunner.Dispose();
                    _deviceInfoRunner = null;
                }
                if (_tokenCreateRunner != null)
                {
                    _tokenCreateRunner.Dispose();
                    _tokenCreateRunner = null;
                }
                if (_tokenInfoRunner != null)
                {
                    _tokenInfoRunner.Dispose();
                    _tokenInfoRunner = null;
                }
                if (_tokenRemoveRunner != null)
                {
                    _tokenRemoveRunner.Dispose();
                    _tokenRemoveRunner = null;
                }
                if (_tokenUploadRunner != null)
                {
                    _tokenUploadRunner.Dispose();
                    _tokenUploadRunner = null;
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
            this.bttTokenCreate = new System.Windows.Forms.Button();
            this.txtDebugTokenLog = new System.Windows.Forms.TextBox();
            this.bttTokenBrowse = new System.Windows.Forms.Button();
            this.txtDebugTokenPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bttOK = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bttAdd = new System.Windows.Forms.Button();
            this.bttRemove = new System.Windows.Forms.Button();
            this.bttUpload = new System.Windows.Forms.Button();
            this.txtDeviceLog = new System.Windows.Forms.TextBox();
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.bttDeviceLoad = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttTokenCreate);
            this.groupBox1.Controls.Add(this.txtDebugTokenLog);
            this.groupBox1.Controls.Add(this.bttTokenBrowse);
            this.groupBox1.Controls.Add(this.txtDebugTokenPath);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(523, 145);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Debug Token";
            // 
            // bttTokenCreate
            // 
            this.bttTokenCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttTokenCreate.Location = new System.Drawing.Point(442, 51);
            this.bttTokenCreate.Name = "bttTokenCreate";
            this.bttTokenCreate.Size = new System.Drawing.Size(75, 23);
            this.bttTokenCreate.TabIndex = 3;
            this.bttTokenCreate.Text = "Cr&eate...";
            this.bttTokenCreate.UseVisualStyleBackColor = true;
            this.bttTokenCreate.Click += new System.EventHandler(this.bttTokenCreate_Click);
            // 
            // txtDebugTokenLog
            // 
            this.txtDebugTokenLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugTokenLog.Location = new System.Drawing.Point(20, 51);
            this.txtDebugTokenLog.Multiline = true;
            this.txtDebugTokenLog.Name = "txtDebugTokenLog";
            this.txtDebugTokenLog.ReadOnly = true;
            this.txtDebugTokenLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDebugTokenLog.Size = new System.Drawing.Size(416, 88);
            this.txtDebugTokenLog.TabIndex = 5;
            // 
            // bttTokenBrowse
            // 
            this.bttTokenBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttTokenBrowse.Location = new System.Drawing.Point(442, 22);
            this.bttTokenBrowse.Name = "bttTokenBrowse";
            this.bttTokenBrowse.Size = new System.Drawing.Size(75, 23);
            this.bttTokenBrowse.TabIndex = 2;
            this.bttTokenBrowse.Text = "&Open...";
            this.bttTokenBrowse.UseVisualStyleBackColor = true;
            this.bttTokenBrowse.Click += new System.EventHandler(this.bttTokenBrowse_Click);
            // 
            // txtDebugTokenPath
            // 
            this.txtDebugTokenPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDebugTokenPath.Location = new System.Drawing.Point(75, 22);
            this.txtDebugTokenPath.Name = "txtDebugTokenPath";
            this.txtDebugTokenPath.ReadOnly = true;
            this.txtDebugTokenPath.Size = new System.Drawing.Size(361, 20);
            this.txtDebugTokenPath.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Path:";
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttOK.Location = new System.Drawing.Point(460, 381);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 3;
            this.bttOK.Text = "&Close";
            this.bttOK.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.bttAdd);
            this.groupBox2.Controls.Add(this.bttRemove);
            this.groupBox2.Controls.Add(this.bttUpload);
            this.groupBox2.Controls.Add(this.txtDeviceLog);
            this.groupBox2.Controls.Add(this.cmbDevices);
            this.groupBox2.Controls.Add(this.bttDeviceLoad);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Location = new System.Drawing.Point(12, 163);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(523, 212);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Onto Device";
            // 
            // bttAdd
            // 
            this.bttAdd.Location = new System.Drawing.Point(270, 19);
            this.bttAdd.Name = "bttAdd";
            this.bttAdd.Size = new System.Drawing.Size(30, 23);
            this.bttAdd.TabIndex = 6;
            this.bttAdd.Text = "+";
            this.bttAdd.UseVisualStyleBackColor = true;
            this.bttAdd.Click += new System.EventHandler(this.bttAdd_Click);
            // 
            // bttRemove
            // 
            this.bttRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttRemove.Enabled = false;
            this.bttRemove.Location = new System.Drawing.Point(442, 129);
            this.bttRemove.Name = "bttRemove";
            this.bttRemove.Size = new System.Drawing.Size(75, 23);
            this.bttRemove.TabIndex = 4;
            this.bttRemove.Text = "Re&move";
            this.bttRemove.UseVisualStyleBackColor = true;
            this.bttRemove.Click += new System.EventHandler(this.bttRemove_Click);
            // 
            // bttUpload
            // 
            this.bttUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttUpload.Enabled = false;
            this.bttUpload.Location = new System.Drawing.Point(442, 46);
            this.bttUpload.Name = "bttUpload";
            this.bttUpload.Size = new System.Drawing.Size(75, 23);
            this.bttUpload.TabIndex = 2;
            this.bttUpload.Text = "&Upload";
            this.bttUpload.UseVisualStyleBackColor = true;
            this.bttUpload.Click += new System.EventHandler(this.bttUpload_Click);
            // 
            // txtDeviceLog
            // 
            this.txtDeviceLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDeviceLog.Location = new System.Drawing.Point(20, 46);
            this.txtDeviceLog.Multiline = true;
            this.txtDeviceLog.Name = "txtDeviceLog";
            this.txtDeviceLog.ReadOnly = true;
            this.txtDeviceLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDeviceLog.Size = new System.Drawing.Size(416, 160);
            this.txtDeviceLog.TabIndex = 5;
            // 
            // cmbDevices
            // 
            this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevices.Enabled = false;
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Location = new System.Drawing.Point(75, 19);
            this.cmbDevices.MaxDropDownItems = 25;
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.Size = new System.Drawing.Size(189, 21);
            this.cmbDevices.TabIndex = 1;
            this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
            // 
            // bttDeviceLoad
            // 
            this.bttDeviceLoad.Enabled = false;
            this.bttDeviceLoad.Location = new System.Drawing.Point(442, 75);
            this.bttDeviceLoad.Name = "bttDeviceLoad";
            this.bttDeviceLoad.Size = new System.Drawing.Size(75, 23);
            this.bttDeviceLoad.TabIndex = 3;
            this.bttDeviceLoad.Text = "Re&load info";
            this.bttDeviceLoad.UseVisualStyleBackColor = true;
            this.bttDeviceLoad.Click += new System.EventHandler(this.bttDeviceLoad_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Target:";
            // 
            // lblError
            // 
            this.lblError.ForeColor = System.Drawing.Color.Red;
            this.lblError.Location = new System.Drawing.Point(30, 378);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(418, 35);
            this.lblError.TabIndex = 2;
            this.lblError.Text = "- error -";
            this.lblError.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblError.Visible = false;
            // 
            // DebugTokenDeploymentForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttOK;
            this.ClientSize = new System.Drawing.Size(547, 411);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.bttOK);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DebugTokenDeploymentForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Debug Token Deployment";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttTokenCreate;
        private System.Windows.Forms.TextBox txtDebugTokenLog;
        private System.Windows.Forms.Button bttTokenBrowse;
        private System.Windows.Forms.TextBox txtDebugTokenPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button bttDeviceLoad;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbDevices;
        private System.Windows.Forms.TextBox txtDeviceLog;
        private System.Windows.Forms.Button bttUpload;
        private System.Windows.Forms.Button bttRemove;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.Button bttAdd;
    }
}