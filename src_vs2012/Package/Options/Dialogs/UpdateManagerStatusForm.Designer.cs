namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class UpdateManagerStatusForm
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
            this.components = new System.ComponentModel.Container();
            this.bttClose = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttAbort = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblActionLog2 = new System.Windows.Forms.Label();
            this.lblActionLog1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bttDelete = new System.Windows.Forms.Button();
            this.listActions = new System.Windows.Forms.ListBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttClose
            // 
            this.bttClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttClose.Location = new System.Drawing.Point(504, 316);
            this.bttClose.Name = "bttClose";
            this.bttClose.Size = new System.Drawing.Size(75, 23);
            this.bttClose.TabIndex = 0;
            this.bttClose.Text = "&Close";
            this.bttClose.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttAbort);
            this.groupBox1.Controls.Add(this.progressBar);
            this.groupBox1.Controls.Add(this.lblActionLog2);
            this.groupBox1.Controls.Add(this.lblActionLog1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(567, 122);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Current";
            // 
            // bttAbort
            // 
            this.bttAbort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttAbort.Enabled = false;
            this.bttAbort.Location = new System.Drawing.Point(486, 78);
            this.bttAbort.Name = "bttAbort";
            this.bttAbort.Size = new System.Drawing.Size(75, 23);
            this.bttAbort.TabIndex = 3;
            this.bttAbort.Text = "&Abort";
            this.bttAbort.UseVisualStyleBackColor = true;
            this.bttAbort.Click += new System.EventHandler(this.bttAbort_Click);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(12, 78);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(468, 23);
            this.progressBar.TabIndex = 2;
            // 
            // lblActionLog2
            // 
            this.lblActionLog2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActionLog2.AutoEllipsis = true;
            this.lblActionLog2.Location = new System.Drawing.Point(9, 49);
            this.lblActionLog2.Name = "lblActionLog2";
            this.lblActionLog2.Size = new System.Drawing.Size(552, 13);
            this.lblActionLog2.TabIndex = 1;
            this.lblActionLog2.Text = "-";
            // 
            // lblActionLog1
            // 
            this.lblActionLog1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblActionLog1.AutoEllipsis = true;
            this.lblActionLog1.Location = new System.Drawing.Point(9, 25);
            this.lblActionLog1.Name = "lblActionLog1";
            this.lblActionLog1.Size = new System.Drawing.Size(552, 13);
            this.lblActionLog1.TabIndex = 0;
            this.lblActionLog1.Text = "-";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.bttDelete);
            this.groupBox2.Controls.Add(this.listActions);
            this.groupBox2.Location = new System.Drawing.Point(12, 140);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(567, 170);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Pending";
            // 
            // bttDelete
            // 
            this.bttDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttDelete.Enabled = false;
            this.bttDelete.Location = new System.Drawing.Point(486, 25);
            this.bttDelete.Name = "bttDelete";
            this.bttDelete.Size = new System.Drawing.Size(75, 23);
            this.bttDelete.TabIndex = 1;
            this.bttDelete.Text = "&Delete";
            this.bttDelete.UseVisualStyleBackColor = true;
            this.bttDelete.Click += new System.EventHandler(this.bttDelete_Click);
            // 
            // listActions
            // 
            this.listActions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listActions.FormattingEnabled = true;
            this.listActions.Location = new System.Drawing.Point(12, 25);
            this.listActions.Name = "listActions";
            this.listActions.Size = new System.Drawing.Size(468, 134);
            this.listActions.TabIndex = 0;
            this.listActions.SelectedIndexChanged += new System.EventHandler(this.listActions_SelectedIndexChanged);
            // 
            // UpdateManagerStatusForm
            // 
            this.AcceptButton = this.bttClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttClose;
            this.ClientSize = new System.Drawing.Size(591, 351);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttClose);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UpdateManagerStatusForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Installation Status";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bttClose;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttAbort;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblActionLog2;
        private System.Windows.Forms.Label lblActionLog1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox listActions;
        private System.Windows.Forms.Button bttDelete;
        private System.Windows.Forms.ToolTip toolTip;
    }
}