using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    public partial class AddLocalNdkForm : Form
    {
        public AddLocalNdkForm()
        {
            InitializeComponent();
        }

        #region Properties

        public string NdkName
        {
            get { return txtName.Text; }
            set { txtName.Text = value; }
        }

        public string NdkTargetPath
        {
            get { return txtTargetPath.Text; }
            set { txtTargetPath.Text = value; }
        }

        public string NdkHostPath
        {
            get { return txtHostPath.Text; }
            set { txtHostPath.Text = value; }
        }

        public Version NdkVersion
        {
            get
            {
                if (string.IsNullOrEmpty(txtVersion.Text) || txtVersion.Text.IndexOf('.') < 0)
                    return new Version(10, 0);

                try
                {
                    return new Version(txtVersion.Text);
                }
                catch
                {
                    return new Version(1, 0);
                }
            }
            set { txtVersion.Text = value != null ? value.ToString() : string.Empty; }
        }

        #endregion

        private void bttBrowseHost_Click(object sender, EventArgs e)
        {
            UpdateByPathSelection(txtHostPath);
        }

        private void bttBrowseTarget_Click(object sender, EventArgs e)
        {
            UpdateByPathSelection(txtTargetPath);
        }

        private void UpdateByPathSelection(TextBox sourceControl)
        {
            var folder = DialogHelper.BrowseForFolder(null, "Specify target-path, host-path or the root folder of those two");
            var ndk = NdkInfo.Scan(folder);

            if (ndk == null)
            {
                MessageBoxHelper.Show("Specified folder is not a valid NDK root", folder ?? string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                sourceControl.Text = folder;
            }
            else
            {
                if (string.IsNullOrEmpty(NdkName))
                    NdkName = ndk.Name;
                if (string.IsNullOrEmpty(NdkHostPath))
                    NdkHostPath = ndk.HostPath;
                if (string.IsNullOrEmpty(NdkTargetPath))
                    NdkTargetPath = ndk.TargetPath;
                if (string.IsNullOrEmpty(txtVersion.Text))
                    NdkVersion = ndk.Version;
            }
        }
    }
}
