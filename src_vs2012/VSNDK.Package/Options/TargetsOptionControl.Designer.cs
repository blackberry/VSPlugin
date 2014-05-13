namespace RIM.VSNDK_Package.Options
{
    partial class TargetsOptionControl
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.bttAdd = new System.Windows.Forms.Button();
            this.bttEdit = new System.Windows.Forms.Button();
            this.bttRemove = new System.Windows.Forms.Button();
            this.lnkMoreInfo = new System.Windows.Forms.LinkLabel();
            this.bttDebugToken = new System.Windows.Forms.Button();
            this.listTargets = new System.Windows.Forms.ListView();
            this.columnActive = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnIP = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.listTargets);
            this.groupBox1.Location = new System.Drawing.Point(3, 46);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(644, 169);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Known Targets";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(737, 33);
            this.label1.TabIndex = 1;
            this.label1.Text = "In order to connect to the device, you must enable the [Development Mode]. It can" +
    " be found at Settings > Security and Privacy > Development Mode.";
            // 
            // bttAdd
            // 
            this.bttAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttAdd.Location = new System.Drawing.Point(653, 51);
            this.bttAdd.Name = "bttAdd";
            this.bttAdd.Size = new System.Drawing.Size(88, 23);
            this.bttAdd.TabIndex = 2;
            this.bttAdd.Text = "&Add...";
            this.bttAdd.UseVisualStyleBackColor = true;
            this.bttAdd.Click += new System.EventHandler(this.bttAdd_Click);
            // 
            // bttEdit
            // 
            this.bttEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttEdit.Enabled = false;
            this.bttEdit.Location = new System.Drawing.Point(653, 80);
            this.bttEdit.Name = "bttEdit";
            this.bttEdit.Size = new System.Drawing.Size(88, 23);
            this.bttEdit.TabIndex = 2;
            this.bttEdit.Text = "&Edit...";
            this.bttEdit.UseVisualStyleBackColor = true;
            // 
            // bttRemove
            // 
            this.bttRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttRemove.Enabled = false;
            this.bttRemove.Location = new System.Drawing.Point(653, 109);
            this.bttRemove.Name = "bttRemove";
            this.bttRemove.Size = new System.Drawing.Size(88, 23);
            this.bttRemove.TabIndex = 2;
            this.bttRemove.Text = "&Remove";
            this.bttRemove.UseVisualStyleBackColor = true;
            // 
            // lnkMoreInfo
            // 
            this.lnkMoreInfo.AutoSize = true;
            this.lnkMoreInfo.Location = new System.Drawing.Point(0, 28);
            this.lnkMoreInfo.Name = "lnkMoreInfo";
            this.lnkMoreInfo.Size = new System.Drawing.Size(60, 13);
            this.lnkMoreInfo.TabIndex = 3;
            this.lnkMoreInfo.TabStop = true;
            this.lnkMoreInfo.Text = "More info...";
            this.lnkMoreInfo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkMoreInfo_LinkClicked);
            // 
            // bttDebugToken
            // 
            this.bttDebugToken.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttDebugToken.Enabled = false;
            this.bttDebugToken.Location = new System.Drawing.Point(653, 167);
            this.bttDebugToken.Name = "bttDebugToken";
            this.bttDebugToken.Size = new System.Drawing.Size(88, 48);
            this.bttDebugToken.TabIndex = 2;
            this.bttDebugToken.Text = "&Upload debug token...";
            this.bttDebugToken.UseVisualStyleBackColor = true;
            // 
            // listTargets
            // 
            this.listTargets.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listTargets.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnActive,
            this.columnName,
            this.columnIP});
            this.listTargets.Location = new System.Drawing.Point(6, 19);
            this.listTargets.Name = "listTargets";
            this.listTargets.Size = new System.Drawing.Size(632, 144);
            this.listTargets.TabIndex = 0;
            this.listTargets.UseCompatibleStateImageBehavior = false;
            // 
            // columnActive
            // 
            this.columnActive.Text = "A";
            this.columnActive.Width = 20;
            // 
            // columnName
            // 
            this.columnName.Text = "Name";
            this.columnName.Width = 80;
            // 
            // columnIP
            // 
            this.columnIP.Text = "IP";
            // 
            // TargetsOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lnkMoreInfo);
            this.Controls.Add(this.bttDebugToken);
            this.Controls.Add(this.bttRemove);
            this.Controls.Add(this.bttEdit);
            this.Controls.Add(this.bttAdd);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Name = "TargetsOptionControl";
            this.Size = new System.Drawing.Size(744, 223);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttAdd;
        private System.Windows.Forms.Button bttEdit;
        private System.Windows.Forms.Button bttRemove;
        private System.Windows.Forms.LinkLabel lnkMoreInfo;
        private System.Windows.Forms.Button bttDebugToken;
        private System.Windows.Forms.ListView listTargets;
        private System.Windows.Forms.ColumnHeader columnActive;
        private System.Windows.Forms.ColumnHeader columnName;
        private System.Windows.Forms.ColumnHeader columnIP;

    }
}
