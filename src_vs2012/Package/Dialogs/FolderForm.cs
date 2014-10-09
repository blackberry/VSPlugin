using System.Windows.Forms;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Dialogs
{
    internal partial class FolderForm : Form
    {
        public FolderForm(string title)
        {
            InitializeComponent();

            Text = title;
        }

        #region Properties

        public string FolderLocation
        {
            get { return txtLocation.Text; }
            set { txtLocation.Text = value; }
        }

        public string FolderName
        {
            get { return txtFolderName.Text; }
            set { txtFolderName.Text = value; }
        }

        #endregion

        private void bttOK_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(FolderName))
            {
                MessageBoxHelper.Show("Name cannot be empty", null, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ActiveControl = txtFolderName;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
