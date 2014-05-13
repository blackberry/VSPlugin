namespace RIM.VSNDK_Package.Options
{
    partial class LogsOptionControl
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
            this.chbInjectLogs = new System.Windows.Forms.CheckBox();
            this.chbLimitLogs = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtLogPath = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.numLogLimit = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numLogLimit)).BeginInit();
            this.SuspendLayout();
            // 
            // chbInjectLogs
            // 
            this.chbInjectLogs.AutoSize = true;
            this.chbInjectLogs.Location = new System.Drawing.Point(6, 76);
            this.chbInjectLogs.Name = "chbInjectLogs";
            this.chbInjectLogs.Size = new System.Drawing.Size(213, 17);
            this.chbInjectLogs.TabIndex = 5;
            this.chbInjectLogs.Text = "Inject device logs into standard console";
            this.chbInjectLogs.UseVisualStyleBackColor = true;
            // 
            // chbLimitLogs
            // 
            this.chbLimitLogs.AutoSize = true;
            this.chbLimitLogs.Location = new System.Drawing.Point(6, 44);
            this.chbLimitLogs.Name = "chbLimitLogs";
            this.chbLimitLogs.Size = new System.Drawing.Size(151, 17);
            this.chbLimitLogs.TabIndex = 3;
            this.chbLimitLogs.Text = "Limit number of old logs to:";
            this.chbLimitLogs.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Logs directory:";
            // 
            // txtLogPath
            // 
            this.txtLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogPath.Location = new System.Drawing.Point(85, 5);
            this.txtLogPath.Name = "txtLogPath";
            this.txtLogPath.Size = new System.Drawing.Size(439, 20);
            this.txtLogPath.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(530, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Browse...";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // numLogLimit
            // 
            this.numLogLimit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numLogLimit.Location = new System.Drawing.Point(478, 41);
            this.numLogLimit.Name = "numLogLimit";
            this.numLogLimit.Size = new System.Drawing.Size(46, 20);
            this.numLogLimit.TabIndex = 4;
            this.numLogLimit.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // LogsOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numLogLimit);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.txtLogPath);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chbLimitLogs);
            this.Controls.Add(this.chbInjectLogs);
            this.Name = "LogsOptionControl";
            this.Size = new System.Drawing.Size(608, 207);
            ((System.ComponentModel.ISupportInitialize)(this.numLogLimit)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chbInjectLogs;
        private System.Windows.Forms.CheckBox chbLimitLogs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtLogPath;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.NumericUpDown numLogLimit;
    }
}
