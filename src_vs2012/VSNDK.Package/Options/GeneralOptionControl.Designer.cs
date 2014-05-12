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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtToolsPath = new System.Windows.Forms.TextBox();
            this.bttNdkBrowse = new System.Windows.Forms.Button();
            this.bttToolsBrowse = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
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
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.bttToolsBrowse);
            this.groupBox1.Controls.Add(this.bttNdkBrowse);
            this.groupBox1.Controls.Add(this.txtToolsPath);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtNdkPath);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(616, 122);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Locations";
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
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(109, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "(Visual Studio Edition)";
            // 
            // txtToolsPath
            // 
            this.txtToolsPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtToolsPath.Location = new System.Drawing.Point(91, 80);
            this.txtToolsPath.Name = "txtToolsPath";
            this.txtToolsPath.Size = new System.Drawing.Size(429, 20);
            this.txtToolsPath.TabIndex = 5;
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
            // bttToolsBrowse
            // 
            this.bttToolsBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttToolsBrowse.Location = new System.Drawing.Point(527, 80);
            this.bttToolsBrowse.Name = "bttToolsBrowse";
            this.bttToolsBrowse.Size = new System.Drawing.Size(75, 23);
            this.bttToolsBrowse.TabIndex = 6;
            this.bttToolsBrowse.Text = "Browse...";
            this.bttToolsBrowse.UseVisualStyleBackColor = true;
            this.bttToolsBrowse.Click += new System.EventHandler(this.bttToolsBrowse_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Tools Path:";
            // 
            // GeneralOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Name = "GeneralOptionControl";
            this.Size = new System.Drawing.Size(619, 217);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

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
    }
}
