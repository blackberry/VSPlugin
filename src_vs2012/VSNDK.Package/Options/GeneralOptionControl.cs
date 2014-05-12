using System.Windows.Forms;

namespace RIM.VSNDK_Package.Options
{
    public partial class GeneralOptionControl : UserControl
    {
        public GeneralOptionControl()
        {
            InitializeComponent();
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

        #endregion

        private void bttNdkBrowse_Click(object sender, System.EventArgs e)
        {
            txtNdkPath.Text = BrowseForFolder(txtNdkPath.Text, "Browse for NDK folder");
        }

        private void bttToolsBrowse_Click(object sender, System.EventArgs e)
        {
            txtToolsPath.Text = BrowseForFolder(txtToolsPath.Text, "Browse for Tools folder");
        }

        private string BrowseForFolder(string startupPath, string description)
        {
            var browser = new FolderBrowserDialog();
            browser.ShowNewFolderButton = true;
            browser.SelectedPath = startupPath;
            browser.Description = description;

            if (browser.ShowDialog() == DialogResult.OK)
                return browser.SelectedPath;

            return startupPath;
        }
    }
}
