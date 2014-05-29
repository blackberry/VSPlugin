using System;
using System.IO;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class AddLocalNdkForm : Form
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
                    return null;

                try
                {
                    return new Version(txtVersion.Text);
                }
                catch
                {
                    return null;
                }
            }
            set { txtVersion.Text = value != null ? value.ToString() : string.Empty; }
        }

        internal NdkInfo NewNdk
        {
            get;
            private set;
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

        private void bttOK_Click(object sender, EventArgs e)
        {
            // verify input data:
            if (NdkVersion == null)
            {
                MessageBoxHelper.Show("Incorrect field value", "Version", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtVersion;
                return;
            }

            if (string.IsNullOrEmpty(NdkHostPath) || !Directory.Exists(NdkHostPath))
            {
                MessageBoxHelper.Show("Incorrect path or doesn't exist, no way to set it as an NDK root", "Host Path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtHostPath;
                return;
            }

            if (string.IsNullOrEmpty(NdkTargetPath) || !Directory.Exists(NdkTargetPath))
            {
                MessageBoxHelper.Show("Incorrect path or doesn't exist, no way to set it as an NDK root", "Host Target", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtHostPath;
                return;
            }

            // create result
            NewNdk = new NdkInfo(NdkName, NdkVersion, NdkHostPath, NdkTargetPath);
            var existingIndex = PackageViewModel.Instance.IndexOfInstalled(NewNdk);
            var existingNDK = existingIndex >= 0 ? PackageViewModel.Instance.InstalledNDKs[existingIndex] : null;

            if (existingNDK != null)
            {
                if (MessageBoxHelper.Show("Are you sure, you want to add it?\r\nIt won't probably show up to select as duplicates are not allowed.",
                                          "Configuration duplicates \"" + existingNDK.Name + "\"",
                                          MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    NewNdk = null;
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
