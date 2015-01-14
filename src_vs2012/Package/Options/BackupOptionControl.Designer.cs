namespace BlackBerry.Package.Options
{
    partial class BackupOptionControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BackupOptionControl));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttBackup = new System.Windows.Forms.Button();
            this.bttRestore = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttBackup);
            this.groupBox1.Controls.Add(this.bttRestore);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(396, 134);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "BlackBerry Developer Certyficate";
            // 
            // bttBackup
            // 
            this.bttBackup.Location = new System.Drawing.Point(9, 97);
            this.bttBackup.Name = "bttBackup";
            this.bttBackup.Size = new System.Drawing.Size(75, 23);
            this.bttBackup.TabIndex = 12;
            this.bttBackup.Text = "&Backup...";
            this.bttBackup.UseVisualStyleBackColor = true;
            this.bttBackup.Click += new System.EventHandler(this.bttBackup_Click);
            // 
            // bttRestore
            // 
            this.bttRestore.Location = new System.Drawing.Point(90, 97);
            this.bttRestore.Name = "bttRestore";
            this.bttRestore.Size = new System.Drawing.Size(75, 23);
            this.bttRestore.TabIndex = 13;
            this.bttRestore.Text = "R&estore...";
            this.bttRestore.UseVisualStyleBackColor = true;
            this.bttRestore.Click += new System.EventHandler(this.bttRestore_Click);
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(6, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(384, 73);
            this.label4.TabIndex = 11;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // BackupOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "BackupOptionControl";
            this.Size = new System.Drawing.Size(402, 298);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttBackup;
        private System.Windows.Forms.Button bttRestore;
        private System.Windows.Forms.Label label4;
    }
}
