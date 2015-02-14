using System;
using System.IO;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Dialogs
{
    internal partial class RegistrationForm : Form
    {
        private const string ValidationCaption = "Validation";
        private readonly DeveloperDefinition _developer;

        public RegistrationForm(DeveloperDefinition developer, int startupSection)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            _developer = developer;

            InitializeComponent();

            AuthorName = _developer.Name;
            Password = _developer.CskPassword;

            if (_developer.HasCertificate)
                Log(string.Format("Found certificate:\r\n * \"{0}\"", _developer.CertificateFullPath));
            if (_developer.HasToken)
                Log(string.Format("Found BlackBerry 10 token:\r\n * \"{0}\"\r\n * version: {1}\r\n * created at: {2}", _developer.CskTokenFullPath, _developer.Token.Version, _developer.Token.CreatedAtString));
            if (_developer.HasTabletToken)
                Log(string.Format("Found Tablet token:\r\n * \"{0}\"\r\n * version: {1}\r\n * created at: {2}", _developer.TabletCskTokenFullPath, _developer.TabletToken.Version, _developer.TabletToken.CreatedAtString));

            UpdateUI();
            ActivateSection(startupSection);
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
            if (string.IsNullOrEmpty(text))
                return;

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
                MessageBoxHelper.Show("Author Name can not be empty.", ValidationCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            bool registered = _developer.IsBB10Registered;

            lblRegistration.Visible = registered;
            lblTokenExpiration.Visible = _developer.HasToken;
            lblTokenExpiration.Text = _developer.HasToken ? "Token expires at: " + _developer.Token.ValidDateString : string.Empty;

            txtCertName.Text = _developer.CertificateFileName;
            bttCreateToken.Enabled = true;
            bttImportCertificate.Enabled = true;
            bttCreateCertificate.Enabled = !_developer.HasCertificate;
        }

        private void UpdateActionButtons(bool enabled)
        {
            bttCreateCertificate.Enabled = enabled;
            bttCreateSigner.Enabled = enabled;
            bttCreateToken.Enabled = enabled;
        }

        private void ActivateSection(int index)
        {
            cmbSections.SelectedIndex = index;
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
                    MessageBoxHelper.Show("Failed to obtain the BlackBerry ID token.\r\nCheck your Internet connection", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            UpdateActionButtons(false);
            Log("Starting registration..." + Environment.NewLine);

            // create CSK file:
            bool successful;
            string certificateFileName;
            using (var runner = new KeyToolRegisterRunner(txtCsjPin.Text, txtCskPassword.Text, txtRdkPath.Text, txtPbdtPath.Text))
            {
                successful = runner.Execute();
                certificateFileName = runner.CertificateFileName;

                // print results:
                if (!string.IsNullOrEmpty(runner.LastOutput))
                    Log(runner.LastOutput);
            }

            // refresh the UI:
            Log("Registration finished.");
            _developer.InvalidateTokens();
            UpdateUI();

            // and save the password:
            if (successful)
            {
                _developer.UpdateCertificate(certificateFileName, txtCskPassword.Text, true);

                if (_developer.IsTabletRegistered)
                {
                    // update the name of the developer (publisher):
                    txtName.Text = _developer.UpdateName(null);
                    MessageBoxHelper.Show("You have successfully registered your BlackBerry Tablet Signer", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBoxHelper.Show("Sorry, something unexpected has happened. Verify logs, unregister and try again.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBoxHelper.Show("Failed to create BlackBerry Tablet Signer. Examine logs for more details.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void bttImportCertificate_Click(object sender, EventArgs e)
        {
            if (CertHelper.Import(_developer))
            {
                AuthorName = _developer.Name;
                Password = _developer.CskPassword;

                UpdateUI();
            }
        }

        private void bttNavigate_Click(object sender, EventArgs e)
        {
            // open the folder, where all the certificates are placed:
            DialogHelper.StartExplorer(_developer.DataPath);
        }

        private void bttRefresh_Click(object sender, EventArgs e)
        {
            if (CertHelper.ReloadAuthor(_developer))
            {
                AuthorName = _developer.Name;
                Password = _developer.CskPassword;
            }
        }

        private void bttCreateCertificate_Click(object sender, EventArgs e)
        {
            if (!CheckPassword(txtPassword, txtConfirmPassword, null))
                return;
            if (!CheckName(txtName))
                return;

            UpdateActionButtons(false);

            using (var runner = new KeyToolGenRunner(AuthorName, Password, null))
            {
                Log("Started certificate generation..." + Environment.NewLine);
                var success = runner.Execute();

                // print results:
                if (runner.ExitCode != 0)
                    Log("Exit code: " + runner.ExitCode);
                if (!string.IsNullOrEmpty(runner.LastError))
                    Log(runner.LastError);
                if (!string.IsNullOrEmpty(runner.LastOutput))
                    Log(runner.LastOutput);

                Log("Certificate creation finished");

                // show result message:
                if (success && string.IsNullOrEmpty(runner.LastError))
                {
                    _developer.UpdateCertificate(runner.CertificateFileName, txtCskPassword.Text, true);

                    MessageBoxHelper.Show("Keystore certificate has been successfully created", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    if (!string.IsNullOrEmpty(runner.LastError))
                    {
                        MessageBoxHelper.Show("Failed to create developer certificate", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            UpdateUI();
        }

        private void cmbSections_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbSections.SelectedIndex)
            {
                case 0:
                    groupBlackBerry10.Visible = true;
                    groupTablet.Visible = false;
                    ActiveControl = txtName;
                    break;
                case 1:
                    groupBlackBerry10.Visible = false;
                    groupTablet.Visible = true;
                    ActiveControl = txtRdkPath;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported index to select");
            }
        }
    }
}
