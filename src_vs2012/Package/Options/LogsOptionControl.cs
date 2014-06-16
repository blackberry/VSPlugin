using System.Windows.Forms;

namespace RIM.VSNDK_Package.Options
{
    public partial class LogsOptionControl : UserControl
    {
        public LogsOptionControl()
        {
            InitializeComponent();
        }

        #region Properties

        public bool InjectLogs
        {
            get { return chbInjectLogs.Checked; }
            set { chbInjectLogs.Checked = value; }
        }

        public bool LimitLogs
        {
            get { return chbLimitLogs.Checked; }
            set { chbLimitLogs.Checked = value; }
        }

        public int LimitCount
        {
            get { return (int) numLogLimit.Value; }
            set { numLogLimit.Value = value; }
        }

        public string Path
        {
            get { return txtLogPath.Text; }
            set { txtLogPath.Text = value; }
        }

        #endregion

        private void bttBrowse_Click(object sender, System.EventArgs e)
        {
            txtLogPath.Text = DialogHelper.BrowseForFolder(txtLogPath.Text, "Browse for Logs folder");
        }
    }
}
