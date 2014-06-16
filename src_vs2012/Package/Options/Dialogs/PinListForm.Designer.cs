namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class PinListForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtPIN = new System.Windows.Forms.TextBox();
            this.bttDiscover = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bttAdd = new System.Windows.Forms.Button();
            this.listPINs = new System.Windows.Forms.ListView();
            this.label2 = new System.Windows.Forms.Label();
            this.columnPIN = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.bttClear = new System.Windows.Forms.Button();
            this.bttRemove = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttCancel
            // 
            this.bttCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttCancel.Location = new System.Drawing.Point(312, 275);
            this.bttCancel.Name = "bttCancel";
            this.bttCancel.Size = new System.Drawing.Size(75, 23);
            this.bttCancel.TabIndex = 2;
            this.bttCancel.Text = "&Cancel";
            this.bttCancel.UseVisualStyleBackColor = true;
            // 
            // bttOK
            // 
            this.bttOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bttOK.Location = new System.Drawing.Point(231, 275);
            this.bttOK.Name = "bttOK";
            this.bttOK.Size = new System.Drawing.Size(75, 23);
            this.bttOK.TabIndex = 1;
            this.bttOK.Text = "&OK";
            this.bttOK.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "PIN:";
            // 
            // txtPIN
            // 
            this.txtPIN.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtPIN.Location = new System.Drawing.Point(72, 19);
            this.txtPIN.Name = "txtPIN";
            this.txtPIN.Size = new System.Drawing.Size(216, 20);
            this.txtPIN.TabIndex = 1;
            // 
            // bttDiscover
            // 
            this.bttDiscover.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttDiscover.Location = new System.Drawing.Point(294, 48);
            this.bttDiscover.Name = "bttDiscover";
            this.bttDiscover.Size = new System.Drawing.Size(75, 23);
            this.bttDiscover.TabIndex = 3;
            this.bttDiscover.Text = "&Discover...";
            this.bttDiscover.UseVisualStyleBackColor = true;
            this.bttDiscover.Click += new System.EventHandler(this.bttDiscover_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.bttRemove);
            this.groupBox1.Controls.Add(this.bttClear);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.listPINs);
            this.groupBox1.Controls.Add(this.bttAdd);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.bttDiscover);
            this.groupBox1.Controls.Add(this.txtPIN);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(375, 257);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "New";
            // 
            // bttAdd
            // 
            this.bttAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttAdd.Location = new System.Drawing.Point(294, 19);
            this.bttAdd.Name = "bttAdd";
            this.bttAdd.Size = new System.Drawing.Size(75, 23);
            this.bttAdd.TabIndex = 2;
            this.bttAdd.Text = "&Add";
            this.bttAdd.UseVisualStyleBackColor = true;
            this.bttAdd.Click += new System.EventHandler(this.bttAdd_Click);
            // 
            // listPINs
            // 
            this.listPINs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listPINs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnPIN});
            this.listPINs.FullRowSelect = true;
            this.listPINs.GridLines = true;
            this.listPINs.HideSelection = false;
            this.listPINs.Location = new System.Drawing.Point(72, 84);
            this.listPINs.MultiSelect = false;
            this.listPINs.Name = "listPINs";
            this.listPINs.Size = new System.Drawing.Size(216, 158);
            this.listPINs.TabIndex = 5;
            this.listPINs.UseCompatibleStateImageBehavior = false;
            this.listPINs.View = System.Windows.Forms.View.Details;
            this.listPINs.SelectedIndexChanged += new System.EventHandler(this.listPINs_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "List:";
            // 
            // columnPIN
            // 
            this.columnPIN.Text = "PINs";
            this.columnPIN.Width = 180;
            // 
            // bttClear
            // 
            this.bttClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttClear.Enabled = false;
            this.bttClear.Location = new System.Drawing.Point(294, 113);
            this.bttClear.Name = "bttClear";
            this.bttClear.Size = new System.Drawing.Size(75, 23);
            this.bttClear.TabIndex = 7;
            this.bttClear.Text = "&Clear";
            this.bttClear.UseVisualStyleBackColor = true;
            this.bttClear.Click += new System.EventHandler(this.bttClear_Click);
            // 
            // bttRemove
            // 
            this.bttRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bttRemove.Enabled = false;
            this.bttRemove.Location = new System.Drawing.Point(294, 84);
            this.bttRemove.Name = "bttRemove";
            this.bttRemove.Size = new System.Drawing.Size(75, 23);
            this.bttRemove.TabIndex = 6;
            this.bttRemove.Text = "&Remove";
            this.bttRemove.UseVisualStyleBackColor = true;
            this.bttRemove.Click += new System.EventHandler(this.bttRemove_Click);
            // 
            // PinListForm
            // 
            this.AcceptButton = this.bttOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttCancel;
            this.ClientSize = new System.Drawing.Size(399, 310);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.bttOK);
            this.Controls.Add(this.bttCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PinListForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PIN Editor";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button bttCancel;
        private System.Windows.Forms.Button bttOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtPIN;
        private System.Windows.Forms.Button bttDiscover;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button bttAdd;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListView listPINs;
        private System.Windows.Forms.ColumnHeader columnPIN;
        private System.Windows.Forms.Button bttRemove;
        private System.Windows.Forms.Button bttClear;
    }
}