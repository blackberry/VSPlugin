namespace BlackBerry.Package.Options
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
            this.bttBrowse = new System.Windows.Forms.Button();
            this.numLogLimit = new System.Windows.Forms.NumericUpDown();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtSlog2BufferSets = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbSlog2Formatter = new System.Windows.Forms.ComboBox();
            this.cmbSlog2Level = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cmbLogsInterval = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chbDebuggedOnly = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.numLogLimit)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // chbInjectLogs
            // 
            this.chbInjectLogs.AutoSize = true;
            this.chbInjectLogs.Checked = true;
            this.chbInjectLogs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbInjectLogs.Location = new System.Drawing.Point(9, 19);
            this.chbInjectLogs.Name = "chbInjectLogs";
            this.chbInjectLogs.Size = new System.Drawing.Size(216, 17);
            this.chbInjectLogs.TabIndex = 0;
            this.chbInjectLogs.Text = "Inject logs into standard \'Debug\' window";
            this.chbInjectLogs.UseVisualStyleBackColor = true;
            // 
            // chbLimitLogs
            // 
            this.chbLimitLogs.AutoSize = true;
            this.chbLimitLogs.Checked = true;
            this.chbLimitLogs.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chbLimitLogs.Location = new System.Drawing.Point(9, 56);
            this.chbLimitLogs.Name = "chbLimitLogs";
            this.chbLimitLogs.Size = new System.Drawing.Size(167, 17);
            this.chbLimitLogs.TabIndex = 3;
            this.chbLimitLogs.Text = "Limit number of old log files to:";
            this.chbLimitLogs.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Logs directory:";
            // 
            // txtLogPath
            // 
            this.txtLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLogPath.Location = new System.Drawing.Point(88, 25);
            this.txtLogPath.Name = "txtLogPath";
            this.txtLogPath.Size = new System.Drawing.Size(435, 20);
            this.txtLogPath.TabIndex = 1;
            // 
            // bttBrowse
            // 
            this.bttBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttBrowse.Location = new System.Drawing.Point(529, 23);
            this.bttBrowse.Name = "bttBrowse";
            this.bttBrowse.Size = new System.Drawing.Size(75, 23);
            this.bttBrowse.TabIndex = 2;
            this.bttBrowse.Text = "Browse...";
            this.bttBrowse.UseVisualStyleBackColor = true;
            this.bttBrowse.Click += new System.EventHandler(this.bttBrowse_Click);
            // 
            // numLogLimit
            // 
            this.numLogLimit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.numLogLimit.Location = new System.Drawing.Point(477, 55);
            this.numLogLimit.Name = "numLogLimit";
            this.numLogLimit.Size = new System.Drawing.Size(46, 20);
            this.numLogLimit.TabIndex = 4;
            this.numLogLimit.Value = new decimal(new int[] {
            25,
            0,
            0,
            0});
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.numLogLimit);
            this.groupBox1.Controls.Add(this.txtLogPath);
            this.groupBox1.Controls.Add(this.chbLimitLogs);
            this.groupBox1.Controls.Add(this.bttBrowse);
            this.groupBox1.Location = new System.Drawing.Point(6, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(610, 80);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Host";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.txtSlog2BufferSets);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.cmbSlog2Formatter);
            this.groupBox2.Controls.Add(this.cmbSlog2Level);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.cmbLogsInterval);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.chbDebuggedOnly);
            this.groupBox2.Controls.Add(this.chbInjectLogs);
            this.groupBox2.Location = new System.Drawing.Point(6, 89);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(610, 204);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Device";
            // 
            // txtSlog2BufferSets
            // 
            this.txtSlog2BufferSets.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSlog2BufferSets.Location = new System.Drawing.Point(121, 163);
            this.txtSlog2BufferSets.Name = "txtSlog2BufferSets";
            this.txtSlog2BufferSets.Size = new System.Drawing.Size(473, 20);
            this.txtSlog2BufferSets.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 166);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(87, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "slog2 buffer-sets:";
            // 
            // cmbSlog2Formatter
            // 
            this.cmbSlog2Formatter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbSlog2Formatter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSlog2Formatter.FormattingEnabled = true;
            this.cmbSlog2Formatter.Items.AddRange(new object[] {
            "message-only",
            "~ + message",
            "# + message",
            "PID + message",
            "appID + message",
            "buffer + message",
            "appID + buffer + message",
            "PID + buffer + message",
            "PID + appID + message"});
            this.cmbSlog2Formatter.Location = new System.Drawing.Point(121, 133);
            this.cmbSlog2Formatter.Name = "cmbSlog2Formatter";
            this.cmbSlog2Formatter.Size = new System.Drawing.Size(402, 21);
            this.cmbSlog2Formatter.TabIndex = 7;
            // 
            // cmbSlog2Level
            // 
            this.cmbSlog2Level.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSlog2Level.FormattingEnabled = true;
            this.cmbSlog2Level.Items.AddRange(new object[] {
            "nothing",
            "applications",
            "system"});
            this.cmbSlog2Level.Location = new System.Drawing.Point(121, 103);
            this.cmbSlog2Level.Name = "cmbSlog2Level";
            this.cmbSlog2Level.Size = new System.Drawing.Size(117, 21);
            this.cmbSlog2Level.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 136);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(79, 13);
            this.label5.TabIndex = 6;
            this.label5.Text = "slog2 formatter:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(99, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "slog2 capture level:";
            // 
            // cmbLogsInterval
            // 
            this.cmbLogsInterval.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLogsInterval.FormattingEnabled = true;
            this.cmbLogsInterval.Location = new System.Drawing.Point(121, 73);
            this.cmbLogsInterval.Name = "cmbLogsInterval";
            this.cmbLogsInterval.Size = new System.Drawing.Size(117, 21);
            this.cmbLogsInterval.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Console logs interval:";
            // 
            // chbDebuggedOnly
            // 
            this.chbDebuggedOnly.AutoSize = true;
            this.chbDebuggedOnly.Location = new System.Drawing.Point(9, 45);
            this.chbDebuggedOnly.Name = "chbDebuggedOnly";
            this.chbDebuggedOnly.Size = new System.Drawing.Size(227, 17);
            this.chbDebuggedOnly.TabIndex = 1;
            this.chbDebuggedOnly.Text = "Monitor logs of debugged applications only";
            this.chbDebuggedOnly.UseVisualStyleBackColor = true;
            // 
            // LogsOptionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "LogsOptionControl";
            this.Size = new System.Drawing.Size(619, 296);
            ((System.ComponentModel.ISupportInitialize)(this.numLogLimit)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chbInjectLogs;
        private System.Windows.Forms.CheckBox chbLimitLogs;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtLogPath;
        private System.Windows.Forms.Button bttBrowse;
        private System.Windows.Forms.NumericUpDown numLogLimit;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox txtSlog2BufferSets;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbSlog2Level;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cmbLogsInterval;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chbDebuggedOnly;
        private System.Windows.Forms.ComboBox cmbSlog2Formatter;
        private System.Windows.Forms.Label label5;
    }
}
