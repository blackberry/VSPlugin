namespace RIM.VSNDK_Package.Options
{
    internal partial class ApiLevelOptionControl
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
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.cmbNDKs = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bttAddLocal = new System.Windows.Forms.Button();
            this.bttInstallNew = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.bttStatus = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtDescription
            // 
            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Location = new System.Drawing.Point(6, 85);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.ReadOnly = true;
            this.txtDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDescription.Size = new System.Drawing.Size(528, 193);
            this.txtDescription.TabIndex = 3;
            // 
            // cmbNDKs
            // 
            this.cmbNDKs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbNDKs.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbNDKs.FormattingEnabled = true;
            this.cmbNDKs.Location = new System.Drawing.Point(6, 58);
            this.cmbNDKs.MaxDropDownItems = 25;
            this.cmbNDKs.Name = "cmbNDKs";
            this.cmbNDKs.Size = new System.Drawing.Size(528, 21);
            this.cmbNDKs.TabIndex = 2;
            this.cmbNDKs.SelectedIndexChanged += new System.EventHandler(this.cmbNDKs_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Current API Level:";
            // 
            // bttAddLocal
            // 
            this.bttAddLocal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttAddLocal.Location = new System.Drawing.Point(459, 284);
            this.bttAddLocal.Name = "bttAddLocal";
            this.bttAddLocal.Size = new System.Drawing.Size(75, 23);
            this.bttAddLocal.TabIndex = 6;
            this.bttAddLocal.Text = "&Add...";
            this.bttAddLocal.UseVisualStyleBackColor = true;
            this.bttAddLocal.Click += new System.EventHandler(this.bttAddLocal_Click);
            // 
            // bttInstallNew
            // 
            this.bttInstallNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttInstallNew.Location = new System.Drawing.Point(378, 284);
            this.bttInstallNew.Name = "bttInstallNew";
            this.bttInstallNew.Size = new System.Drawing.Size(75, 23);
            this.bttInstallNew.TabIndex = 5;
            this.bttInstallNew.Text = "&Install...";
            this.bttInstallNew.UseVisualStyleBackColor = true;
            this.bttInstallNew.Click += new System.EventHandler(this.bttInstallNew_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 11);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(248, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Select the API Level that your application supports.";
            // 
            // bttStatus
            // 
            this.bttStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bttStatus.Location = new System.Drawing.Point(6, 284);
            this.bttStatus.Name = "bttStatus";
            this.bttStatus.Size = new System.Drawing.Size(75, 23);
            this.bttStatus.TabIndex = 4;
            this.bttStatus.Text = "&Status...";
            this.bttStatus.UseVisualStyleBackColor = true;
            this.bttStatus.Click += new System.EventHandler(this.bttStatus_Click);
            // 
            // ApiLevelOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.bttStatus);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.cmbNDKs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.bttAddLocal);
            this.Controls.Add(this.bttInstallNew);
            this.Name = "ApiLevelOptionControl";
            this.Size = new System.Drawing.Size(537, 310);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.ComboBox cmbNDKs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttAddLocal;
        private System.Windows.Forms.Button bttInstallNew;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttStatus;
    }
}
