namespace BlackBerry.Package.Dialogs
{
    internal partial class ImportProjectForm
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
            this.bttOK = new System.Windows.Forms.Button();
            this.bttCancel = new System.Windows.Forms.Button();
            this.txtSourceProject = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtDependencies = new System.Windows.Forms.TextBox();
            this.txtDefines = new System.Windows.Forms.TextBox();
            this.chkAtSourceLocation = new System.Windows.Forms.CheckBox();
            this.chkCopyFiles = new System.Windows.Forms.CheckBox();
            this.txtDestinationName = new System.Windows.Forms.TextBox();
            this.cmbProjects = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.bttFindProject = new System.Windows.Forms.Button();
            this.txtWarnings = new System.Windows.Forms.TextBox();
            this.listFiles = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.Location = new System.Drawing.Point(406, 462);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 0;
            this.bttOK.Text = "&OK";
            this.bttOK.UseVisualStyleBackColor = true;
            this.bttOK.Click += new System.EventHandler(this.bttOK_Click);
            // 
            // bttCancel
            // 
            this.bttCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttCancel.Location = new System.Drawing.Point(487, 462);
            this.bttCancel.Name = "bttCancel";
            this.bttCancel.Size = new System.Drawing.Size(75, 23);
            this.bttCancel.TabIndex = 1;
            this.bttCancel.Text = "&Cancel";
            this.bttCancel.UseVisualStyleBackColor = true;
            // 
            // txtSourceProject
            // 
            this.txtSourceProject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSourceProject.Location = new System.Drawing.Point(103, 22);
            this.txtSourceProject.Name = "txtSourceProject";
            this.txtSourceProject.ReadOnly = true;
            this.txtSourceProject.Size = new System.Drawing.Size(400, 20);
            this.txtSourceProject.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Source project:";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.txtDependencies);
            this.groupBox1.Controls.Add(this.txtDefines);
            this.groupBox1.Controls.Add(this.chkAtSourceLocation);
            this.groupBox1.Controls.Add(this.chkCopyFiles);
            this.groupBox1.Controls.Add(this.txtDestinationName);
            this.groupBox1.Controls.Add(this.cmbProjects);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.bttFindProject);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtSourceProject);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(550, 155);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Project info";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 128);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Libraries:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 102);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Defines:";
            // 
            // txtDependencies
            // 
            this.txtDependencies.Location = new System.Drawing.Point(103, 125);
            this.txtDependencies.Name = "txtDependencies";
            this.txtDependencies.Size = new System.Drawing.Size(400, 20);
            this.txtDependencies.TabIndex = 11;
            // 
            // txtDefines
            // 
            this.txtDefines.Location = new System.Drawing.Point(103, 99);
            this.txtDefines.Name = "txtDefines";
            this.txtDefines.Size = new System.Drawing.Size(400, 20);
            this.txtDefines.TabIndex = 10;
            // 
            // chkAtSourceLocation
            // 
            this.chkAtSourceLocation.AutoSize = true;
            this.chkAtSourceLocation.Checked = true;
            this.chkAtSourceLocation.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAtSourceLocation.Enabled = false;
            this.chkAtSourceLocation.ForeColor = System.Drawing.Color.Red;
            this.chkAtSourceLocation.Location = new System.Drawing.Point(320, 76);
            this.chkAtSourceLocation.Name = "chkAtSourceLocation";
            this.chkAtSourceLocation.Size = new System.Drawing.Size(179, 17);
            this.chkAtSourceLocation.TabIndex = 9;
            this.chkAtSourceLocation.Text = "Create at source project location";
            this.chkAtSourceLocation.UseVisualStyleBackColor = true;
            // 
            // chkCopyFiles
            // 
            this.chkCopyFiles.AutoSize = true;
            this.chkCopyFiles.Checked = true;
            this.chkCopyFiles.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCopyFiles.Location = new System.Drawing.Point(103, 76);
            this.chkCopyFiles.Name = "chkCopyFiles";
            this.chkCopyFiles.Size = new System.Drawing.Size(106, 17);
            this.chkCopyFiles.TabIndex = 8;
            this.chkCopyFiles.Text = "Copy source files";
            this.chkCopyFiles.UseVisualStyleBackColor = true;
            // 
            // txtDestinationName
            // 
            this.txtDestinationName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDestinationName.Location = new System.Drawing.Point(320, 48);
            this.txtDestinationName.Name = "txtDestinationName";
            this.txtDestinationName.ReadOnly = true;
            this.txtDestinationName.Size = new System.Drawing.Size(183, 20);
            this.txtDestinationName.TabIndex = 7;
            this.txtDestinationName.TextChanged += new System.EventHandler(this.txtDestinationName_TextChanged);
            // 
            // cmbProjects
            // 
            this.cmbProjects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbProjects.FormattingEnabled = true;
            this.cmbProjects.Location = new System.Drawing.Point(103, 48);
            this.cmbProjects.Name = "cmbProjects";
            this.cmbProjects.Size = new System.Drawing.Size(211, 21);
            this.cmbProjects.TabIndex = 6;
            this.cmbProjects.SelectedIndexChanged += new System.EventHandler(this.cmbProjects_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 51);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Destination:";
            // 
            // bttFindProject
            // 
            this.bttFindProject.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttFindProject.Location = new System.Drawing.Point(509, 21);
            this.bttFindProject.Name = "bttFindProject";
            this.bttFindProject.Size = new System.Drawing.Size(35, 23);
            this.bttFindProject.TabIndex = 4;
            this.bttFindProject.Text = "...";
            this.bttFindProject.UseVisualStyleBackColor = true;
            this.bttFindProject.Click += new System.EventHandler(this.bttFindProject_Click);
            // 
            // txtWarnings
            // 
            this.txtWarnings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtWarnings.Location = new System.Drawing.Point(12, 412);
            this.txtWarnings.Multiline = true;
            this.txtWarnings.Name = "txtWarnings";
            this.txtWarnings.ReadOnly = true;
            this.txtWarnings.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtWarnings.Size = new System.Drawing.Size(550, 44);
            this.txtWarnings.TabIndex = 6;
            this.txtWarnings.Text = "All colliding files will be overwritten without notice.";
            // 
            // listFiles
            // 
            this.listFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listFiles.CheckBoxes = true;
            this.listFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listFiles.FullRowSelect = true;
            this.listFiles.GridLines = true;
            this.listFiles.HideSelection = false;
            this.listFiles.Location = new System.Drawing.Point(12, 173);
            this.listFiles.Name = "listFiles";
            this.listFiles.Size = new System.Drawing.Size(550, 233);
            this.listFiles.TabIndex = 7;
            this.listFiles.UseCompatibleStateImageBehavior = false;
            this.listFiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Relative location";
            this.columnHeader1.Width = 520;
            // 
            // ImportProjectForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttCancel;
            this.ClientSize = new System.Drawing.Size(574, 497);
            this.Controls.Add(this.listFiles);
            this.Controls.Add(this.txtWarnings);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttCancel);
            this.Controls.Add(this.bttOK);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(460, 360);
            this.Name = "ImportProjectForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import Momentics Project";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.Button bttCancel;
        private System.Windows.Forms.TextBox txtSourceProject;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttFindProject;
        private System.Windows.Forms.CheckBox chkCopyFiles;
        private System.Windows.Forms.TextBox txtDestinationName;
        private System.Windows.Forms.ComboBox cmbProjects;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtWarnings;
        private System.Windows.Forms.CheckBox chkAtSourceLocation;
        private System.Windows.Forms.ListView listFiles;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtDependencies;
        private System.Windows.Forms.TextBox txtDefines;
    }
}