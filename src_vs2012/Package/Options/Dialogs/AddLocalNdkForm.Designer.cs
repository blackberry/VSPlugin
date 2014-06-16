namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class AddLocalNdkForm
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
            this.bttCancel = new System.Windows.Forms.Button();
            this.bttOK = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttBrowseTarget = new System.Windows.Forms.Button();
            this.bttBrowseHost = new System.Windows.Forms.Button();
            this.txtVersion = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtTargetPath = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtHostPath = new System.Windows.Forms.TextBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttCancel
            // 
            this.bttCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttCancel.Location = new System.Drawing.Point(587, 211);
            this.bttCancel.Name = "bttCancel";
            this.bttCancel.Size = new System.Drawing.Size(75, 23);
            this.bttCancel.TabIndex = 2;
            this.bttCancel.Text = "&Cancel";
            this.bttCancel.UseVisualStyleBackColor = true;
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.Location = new System.Drawing.Point(506, 211);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 1;
            this.bttOK.Text = "&OK";
            this.bttOK.UseVisualStyleBackColor = true;
            this.bttOK.Click += new System.EventHandler(this.bttOK_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttBrowseTarget);
            this.groupBox1.Controls.Add(this.bttBrowseHost);
            this.groupBox1.Controls.Add(this.txtVersion);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.txtTargetPath);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtHostPath);
            this.groupBox1.Controls.Add(this.txtName);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(650, 193);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Properties";
            // 
            // bttBrowseTarget
            // 
            this.bttBrowseTarget.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttBrowseTarget.Location = new System.Drawing.Point(564, 123);
            this.bttBrowseTarget.Name = "bttBrowseTarget";
            this.bttBrowseTarget.Size = new System.Drawing.Size(75, 23);
            this.bttBrowseTarget.TabIndex = 8;
            this.bttBrowseTarget.Text = "Browse...";
            this.bttBrowseTarget.UseVisualStyleBackColor = true;
            this.bttBrowseTarget.Click += new System.EventHandler(this.bttBrowseTarget_Click);
            // 
            // bttBrowseHost
            // 
            this.bttBrowseHost.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttBrowseHost.Location = new System.Drawing.Point(564, 91);
            this.bttBrowseHost.Name = "bttBrowseHost";
            this.bttBrowseHost.Size = new System.Drawing.Size(75, 23);
            this.bttBrowseHost.TabIndex = 5;
            this.bttBrowseHost.Text = "Browse...";
            this.bttBrowseHost.UseVisualStyleBackColor = true;
            this.bttBrowseHost.Click += new System.EventHandler(this.bttBrowseHost_Click);
            // 
            // txtVersion
            // 
            this.txtVersion.Location = new System.Drawing.Point(106, 150);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.Size = new System.Drawing.Size(100, 20);
            this.txtVersion.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 153);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(45, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Version:";
            // 
            // txtTargetPath
            // 
            this.txtTargetPath.Location = new System.Drawing.Point(106, 120);
            this.txtTargetPath.Name = "txtTargetPath";
            this.txtTargetPath.Size = new System.Drawing.Size(452, 20);
            this.txtTargetPath.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 64);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Name:";
            // 
            // txtHostPath
            // 
            this.txtHostPath.Location = new System.Drawing.Point(106, 91);
            this.txtHostPath.Name = "txtHostPath";
            this.txtHostPath.Size = new System.Drawing.Size(452, 20);
            this.txtHostPath.TabIndex = 4;
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(106, 61);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(274, 20);
            this.txtName.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Host Path:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 123);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Target Path:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(558, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Specify the location of a local NDK and name it uniquely. Use Browse buttons belo" +
    "w, to automate the whole process.";
            // 
            // AddLocalNdkForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttCancel;
            this.ClientSize = new System.Drawing.Size(674, 246);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttOK);
            this.Controls.Add(this.bttCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddLocalNdkForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Custom NDK";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bttCancel;
        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttBrowseTarget;
        private System.Windows.Forms.Button bttBrowseHost;
        private System.Windows.Forms.TextBox txtVersion;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtTargetPath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtHostPath;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}