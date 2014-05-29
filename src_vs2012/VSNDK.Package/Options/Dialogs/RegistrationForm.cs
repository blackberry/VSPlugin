using System;
using System.IO;
using System.Windows.Forms;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    public partial class RegistrationForm : Form
    {
        private const string ValidationCaption = "Validation";
        private readonly DeveloperDefinition _developer;

        internal RegistrationForm(DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            _developer = developer;

            InitializeComponent();

            Password = _developer.CskPassword;
            AuthorName = _developer.Name;

            UpdateUI();
        }

        #region Properties

        public string AuthorName
        {
            get { return txtName.Text; }
            set { txtName.Text = value; }
        }

        public string Password
        {
            get { return txtPassword.Text; }
            set
            {
                txtPassword.Text = value;
                txtConfirmPassword.Text = value;
                txtCskPassword.Text = value;
                txtCskConfirmPassword.Text = value;
            }
        }

        #endregion

        #region Logging

        private void ClearLogs()
        {
            txtLog.Text = string.Empty;
        }

        private void Log(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(Log), text);
                return;
            }

            txtLog.Text += text + Environment.NewLine;
        }

        #endregion

        #region Input Checks

        /// <summary>
        /// Checks for input password errors. Returns 'true', when all is OK.
        /// </summary>
        private bool CheckPassword(TextBox password, TextBox confirmation, string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "Password";

            if (password != null && string.IsNullOrEmpty(password.Text))
            {
                MessageBoxHelper.Show(name + " can not be empty", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = password;
                return false;
            }

            if (password != null && confirmation != null
                && string.Compare(password.Text, confirmation.Text, StringComparison.CurrentCulture) != 0)
            {
                MessageBoxHelper.Show(name + "s are not matching.", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = confirmation;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for input file name errors. Returns 'true', when all is OK.
        /// </summary>
        private bool CheckPath(TextBox path, string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "file name";

            if (path != null && string.IsNullOrEmpty(path.Text))
            {
                MessageBoxHelper.Show("Specified " + name + " can not be empty", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = path;
                return false;
            }

            if (path != null && !File.Exists(path.Text))
            {
                MessageBoxHelper.Show("Specified " + name + " does not exist. Ensure it points to the right file", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = path;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for author name's errors. Returns 'true', when all is OK.
        /// </summary>
        private bool CheckName(TextBox author)
        {
            if (string.IsNullOrEmpty(author.Text))
            {
                MessageBoxHelper.Show("Author Name can not be empty. Please enter a meaningful text.", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = author;
                return false;
            }

            if (author.Text.Trim().ToUpper().Contains("BLACKBERRY"))
            {
                MessageBoxHelper.Show("\"BlackBerry\" is a reserved word and cannot be used as \"Author Name\"", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ActiveControl = author;
                return false;
            }

            return true;
        }

        #endregion

        private void UpdateUI()
        {
            // tablet section:
            bool tabletRegistered = _developer.IsTabletRegistered;

            lblTabletRegistration.Visible = tabletRegistered;

            bttCreateSigner.Enabled = !tabletRegistered;
            txtRdkPath.Enabled = bttRdkNavigate.Enabled = !tabletRegistered;
            txtPbdtPath.Enabled = bttPbdtNavigate.Enabled = !tabletRegistered;
            txtCsjPin.Enabled = txtCskPassword.Enabled = txtCskConfirmPassword.Enabled = !tabletRegistered;

            // BB10 devices section:

        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            txtConfirmPassword.Text = string.Empty;
            txtCskPassword.Text = txtPassword.Text;
        }

        private void txtCskPassword_TextChanged(object sender, EventArgs e)
        {
            txtCskConfirmPassword.Text = string.Empty;
            txtPassword.Text = txtCskPassword.Text;
        }

        private void bttCreateToken_Click(object sender, EventArgs e)
        {
            if (!CheckPassword(txtPassword, txtConfirmPassword, null))
                return;

            if (File.Exists(_developer.CskTokenFullPath))
            {
                // confirm overwrite operation:
                if (MessageBoxHelper.Show("Overwrite existing BlackBerry ID token?", null, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

            // show token request form:
            var form = new CskRequestForm(null);
            form.StartRequest(txtPassword.Text);

            if (form.ShowDialog() == DialogResult.OK)
            {
                // got the token, so save it in respective location:
                _developer.SaveCskToken(form.Token);

                // log it:
                Log("Created BlackBerry ID token - valid until " + form.Token.ValidDateString);
            }
            else
            {
                if (form.StatusCode != 0)
                {
                    MessageBoxHelper.Show("Failed to obtain the BlackBerry ID token from Security Authority.\r\nPlease check your Internet connection", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            UpdateUI();
        }

        private void lnkMoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://www.blackberry.com/go/codesigning/");
        }

        private void bttRdkNavigate_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.OpenCsjFile(_developer.DataPath, "Opening RDK file");

            if (form.ShowDialog() == DialogResult.OK)
            {
                txtRdkPath.Text = form.FileName;
            }
        }

        private void bttPbdtNavigate_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.OpenCsjFile(_developer.DataPath, "Opening PBDT file");

            if (form.ShowDialog() == DialogResult.OK)
            {
                txtPbdtPath.Text = form.FileName;
            }
        }

        private void bttCreateSigner_Click(object sender, EventArgs e)
        {
            if (!CheckPassword(txtCskPassword, txtCskConfirmPassword, null))
                return;
            if (!CheckPassword(txtCsjPin, null, "PIN"))
                return;

            // check paths:
            if (!CheckPath(txtRdkPath, "RDK path"))
                return;
            if (!CheckPath(txtPbdtPath, "PBDT path"))
                return;
        }
    }
}
