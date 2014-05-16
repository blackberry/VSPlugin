using System.Diagnostics;
using System.Windows.Forms;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.Options
{
    public partial class GeneralOptionControl : UserControl
    {
        public GeneralOptionControl()
        {
            InitializeComponent();

            txtNdkPath.Text = RunnerDefaults.NdkDirectory;
            txtToolsPath.Text = RunnerDefaults.ToolsDirectory;
            txtProfilePath.Text = RunnerDefaults.DataDirectory;
        }

        #region Properties

        public string NdkPath
        {
            get { return txtNdkPath.Text; }
            set { txtNdkPath.Text = value; }
        }

        public string ToolsPath
        {
            get { return txtToolsPath.Text; }
            set { txtToolsPath.Text = value; }
        }

        public string ProfilePath
        {
            get { return txtProfilePath.Text; }
            set { txtProfilePath.Text = value; }
        }

        #endregion

        private void bttNdkBrowse_Click(object sender, System.EventArgs e)
        {
            txtNdkPath.Text = DialogHelper.BrowseForFolder(txtNdkPath.Text, "Browse for NDK folder");
        }

        private void bttToolsBrowse_Click(object sender, System.EventArgs e)
        {
            txtToolsPath.Text = DialogHelper.BrowseForFolder(txtToolsPath.Text, "Browse for Tools folder");
        }

        private void bttOpenProfile_Click(object sender, System.EventArgs e)
        {
            DialogHelper.StartExplorer(ProfilePath);
        }
    }
}
