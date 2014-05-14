using System;
using System.Windows.Forms;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    public partial class PasswordForm : Form
    {
        public PasswordForm()
        {
            InitializeComponent();
        }

        #region Properties

        public string Password
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value; }
        }

        public bool ShouldRemember
        {
            get { return chkRemember.Checked; }
            set { chkRemember.Checked = value; }
        }

        #endregion

        private void bttOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Password))
            {
                MessageBoxHelper.Show("You must specify value for password", "Password error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = txtPassword;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
