using System.ComponentModel;
using System.Windows.Forms;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.Options
{
    public partial class GeneralOptionControl : UserControl
    {
        public GeneralOptionControl()
        {
            InitializeComponent();
            OnReset();
        }

        #region Properties

        [Browsable(false)]
        public string NdkPath
        {
            get { return txtNdkPath.Text; }
            set { txtNdkPath.Text = value; }
        }

        [Browsable(false)]
        public string ToolsPath
        {
            get { return txtToolsPath.Text; }
            set { txtToolsPath.Text = value; }
        }

        [Browsable(false)]
        public string ProfilePath
        {
            get { return txtProfilePath.Text; }
            set { txtProfilePath.Text = value; }
        }

        /// <summary>
        /// Checks if open URL links in internal or external browser.
        /// </summary>
        public bool IsOpeningExternal
        {
            get { return chkOpenInExternal.Checked; }
            set { chkOpenInExternal.Checked = value; }
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

        public void OnReset()
        {
            txtNdkPath.Text = RunnerDefaults.NdkDirectory;
            txtToolsPath.Text = RunnerDefaults.ToolsDirectory;
            txtProfilePath.Text = RunnerDefaults.DataDirectory;
        }
    }
}
