using System;
using System.Windows.Forms;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    public partial class RegistrationForm : Form
    {
        public RegistrationForm()
        {
            InitializeComponent();
        }

        #region Properties

        public string AuthorName
        {
            get { return txtName.Text; }
            set { txtName.Text = value; }
        }

        public string AuthorPassword
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value; }
        }

        #endregion

        private void bttOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthorName))
            {
                MessageBoxHelper.Show("Name can't be empty. Please enter a meaningful text.", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (AuthorName.Trim().ToUpper().Contains("BLACKBERRY"))
            {
                MessageBoxHelper.Show("\"BlackBerry\" is a reserved word and cannot be used as \"Author Name\"", "Invalid name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(AuthorPassword))
            {
                MessageBoxHelper.Show("Password can't be empty. Please enter any text.", "Invalid password", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
