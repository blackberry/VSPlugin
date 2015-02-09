namespace BlackBerry.Package.Dialogs
{
    internal partial class MissingNdkInstalledForm
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
                if (_vm != null)
                {
                    _vm.Dispose();
                    _vm = null;
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
            this.bttClose = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbNDKs = new System.Windows.Forms.ComboBox();
            this.bttInstall = new System.Windows.Forms.Button();
            this.bttStatus = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttClose
            // 
            this.bttClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttClose.Location = new System.Drawing.Point(361, 148);
            this.bttClose.Name = "bttClose";
            this.bttClose.Size = new System.Drawing.Size(75, 23);
            this.bttClose.TabIndex = 0;
            this.bttClose.Text = "&Close";
            this.bttClose.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cmbNDKs);
            this.groupBox1.Controls.Add(this.bttInstall);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(424, 130);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Selection";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(15, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(403, 66);
            this.label1.TabIndex = 2;
            this.label1.Text = "Toolset for building BlackBerry native application is missing. Make sure t" +
    "here is any NDK installed and select it from the list below.";
            // 
            // cmbNDKs
            // 
            this.cmbNDKs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbNDKs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNDKs.FormattingEnabled = true;
            this.cmbNDKs.Location = new System.Drawing.Point(15, 91);
            this.cmbNDKs.Name = "cmbNDKs";
            this.cmbNDKs.Size = new System.Drawing.Size(322, 21);
            this.cmbNDKs.TabIndex = 1;
            this.cmbNDKs.SelectedIndexChanged += new System.EventHandler(this.cmbNDKs_SelectedIndexChanged);
            // 
            // bttInstall
            // 
            this.bttInstall.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttInstall.Location = new System.Drawing.Point(343, 89);
            this.bttInstall.Name = "bttInstall";
            this.bttInstall.Size = new System.Drawing.Size(75, 23);
            this.bttInstall.TabIndex = 0;
            this.bttInstall.Text = "Install...";
            this.bttInstall.UseVisualStyleBackColor = true;
            this.bttInstall.Click += new System.EventHandler(this.bttInstall_Click);
            // 
            // bttStatus
            // 
            this.bttStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttStatus.Location = new System.Drawing.Point(12, 148);
            this.bttStatus.Name = "bttStatus";
            this.bttStatus.Size = new System.Drawing.Size(75, 23);
            this.bttStatus.TabIndex = 2;
            this.bttStatus.Text = "&Status...";
            this.bttStatus.UseVisualStyleBackColor = true;
            this.bttStatus.Click += new System.EventHandler(this.bttStatus_Click);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(15, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(403, 32);
            this.label2.TabIndex = 3;
            this.label2.Text = "NOTE: It might take some time to download everything. You can close this dialog a" +
    "nd continue coding, but you won\'t be able to build and deploy until all is finis" +
    "hed.";
            // 
            // MissingNdkInstalledForm
            // 
            this.AcceptButton = this.bttClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttClose;
            this.ClientSize = new System.Drawing.Size(448, 183);
            this.Controls.Add(this.bttStatus);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MissingNdkInstalledForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Default BlackBerry API Level selection";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bttClose;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbNDKs;
        private System.Windows.Forms.Button bttInstall;
        private System.Windows.Forms.Button bttStatus;
        private System.Windows.Forms.Label label2;
    }
}