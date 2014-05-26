using System;
using System.IO;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    public partial class SigningOptionControl : UserControl
    {
        private readonly SigningOptionViewModel _vm = new SigningOptionViewModel();

        public SigningOptionControl()
        {
            InitializeComponent();
            UpdateUI();
        }

        #region Properties

        public string CertificatePath
        {
            get { return txtCertPath.Text; }
            set { txtCertPath.Text = value; }
        }

        public string Author
        {
            get { return txtAuthor.Text; }
            set { txtAuthor.Text = value; }
        }

        #endregion

        private void UpdateUI()
        {
            CertificatePath = _vm.Developer.CertificateFileName;
            Author = _vm.Developer.Name;

            bttRegister.Enabled = !_vm.Developer.IsRegistered;
            bttUnregister.Enabled = !bttRegister.Enabled;
            bttBackup.Enabled = bttUnregister.Enabled;

            bttNavigate.Enabled = !string.IsNullOrEmpty(CertificatePath);
            bttDeletePassword.Enabled = _vm.Developer.IsPasswordSaved;
            bttRefresh.Enabled = bttNavigate.Enabled;
        }

        /// <summary>
        /// Control has become visible again, when user navigated over Settings.
        /// </summary>
        public void OnActivate()
        {
            UpdateUI();
        }

        private void ReloadAuthor()
        {
            // try to reload info with cached password:
            if (_vm.Developer.HasPassword)
            {
                Author = _vm.Developer.UpdateName(null);
                UpdateUI();

                if (!string.IsNullOrEmpty(Author))
                    return;
            }

            // if it failed, ask for new password:
            var form = new PasswordForm();
            if (form.ShowDialog() != DialogResult.OK)
                return;

            _vm.Developer.UpdatePassword(form.Password, form.ShouldRemember);

            // try again to reload data from certificate:
            Author = _vm.Developer.UpdateName(null);
            UpdateUI();
            VerifyAuthor(Author);
        }

        private void lblMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://www.blackberry.com/go/codesigning/");
        }

        private static void VerifyAuthor(string author)
        {
            if (string.IsNullOrEmpty(author))
            {
                MessageBoxHelper.Show("Unable to load info about author", "Invalid password or certificate file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        internal static void CopyExistingCert(DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            if (string.IsNullOrEmpty(developer.DataPath))
                throw new ArgumentOutOfRangeException("developer");

            // navigate for new certificate:
            var form = DialogHelper.OpenCertFile(developer.DataPath);

            if (form.ShowDialog() == DialogResult.OK && File.Exists(form.FileName))
            {
                // will need to move the file? - ask for confirmation, if one with the same name exists:
                var srcPath = form.FileName;
                var fileName = Path.GetFileName(srcPath);
                var folderName = Path.GetDirectoryName(srcPath);

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(folderName))
                    return;

                // copy the file:
                if (string.Compare(developer.DataPath, folderName, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    var destPath = Path.Combine(developer.DataPath, fileName);
                    if (File.Exists(destPath))
                    {
                        var result = MessageBoxHelper.Show("File \"" + fileName + "\" already exists in certificate storage folder.\r\nDo you want to overwrite it?",
                                                           null, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);

                        if (result == DialogResult.Cancel)
                            return;
                        if (result == DialogResult.No)
                        {
                            // generate new name:
                            fileName = "author-" + DateTime.Now.ToString("yyyy-MM-dd") + ".p12";
                            destPath = Path.Combine(developer.DataPath, fileName);
                        }
                    }

                    try
                    {
                        File.Copy(srcPath, destPath, true);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to copy certificate file \"{0}\"", srcPath);
                        MessageBoxHelper.Show(ex.Message, "Certificate file error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                developer.UpdateCertificate(fileName);

                // ask for password several times:
                while (true)
                {
                    var passForm = new PasswordForm();

                    if (passForm.ShowDialog() == DialogResult.OK)
                    {
                        // load info from new certificate:
                        developer.UpdateName(passForm.Password);

                        // succeeded - yes?
                        if (developer.HasName)
                        {
                            developer.UpdatePassword(passForm.Password, passForm.ShouldRemember);
                            break;
                        }

                        // no - display error and ask again:
                        VerifyAuthor(developer.Name);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void bttChangeCert_Click(object sender, EventArgs e)
        {
            CopyExistingCert(_vm.Developer);
            UpdateUI();
        }

        private void bttNavigate_Click(object sender, EventArgs e)
        {
            var path = _vm.Developer.CertificateFullPath;
            if (string.IsNullOrEmpty(path))
                return;

            DialogHelper.StartExplorerForFile(path);
        }

        private void bttDeletePassword_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Are you sure to delete stored certificate password?\r\n\r\nYou will need to type it in, whenever required.",
                null, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _vm.Developer.ClearPassword();
            UpdateUI();
        }

        private void bttRefresh_Click(object sender, EventArgs e)
        {
            ReloadAuthor();
        }

        private void bttBackup_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.SaveZipFile("Exporting Developer Profile", "profile_backup_" + DateTime.Now.ToString("yyyy-MM-dd") + ".zip");

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (_vm.Developer.BackupProfile(form.FileName))
                {
                    MessageBoxHelper.Show("Developer profile exported", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBoxHelper.Show("Error while exporting developer profile", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                UpdateUI();
            }
        }

        private void bttRestore_Click(object sender, EventArgs e)
        {
            var form = DialogHelper.OpenZipFile("Restoring Developer Profile");

            if (form.ShowDialog() == DialogResult.OK)
            {
                if (_vm.Developer.RestoreProfile(form.FileName))
                {
                    MessageBoxHelper.Show("Developer profile restored", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBoxHelper.Show("Error while importing developer profile", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ReloadAuthor();
            }
        }

        private void bttUnregister_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Do you want to unregister and remove the BlackBerry ID Token file?\r\nThis operation can not be reverted. Make sure you have created a backup.", "UNREGISTRATION!!", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ///////////////////////
                // UNREGISTER
                // this is the only place, where doing it synchronously is OK:
                var runner = new KeyToolRemoveRunner(RunnerDefaults.ToolsDirectory);
                var success = runner.Execute();

                // and delete all profile-related files:
                _vm.Developer.DeleteProfile();
                UpdateUI();

                // finally, show result message:
                if (success && string.IsNullOrEmpty(runner.LastError))
                {
                    if (!string.IsNullOrEmpty(runner.LastOutput))
                    {
                        MessageBoxHelper.Show(runner.LastOutput.Replace("CSK", "BB ID Token"), "Unregistered developer profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(runner.LastError))
                    {
                        MessageBoxHelper.Show(runner.LastError, "Failed to remove developer profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void bttRegister_Click(object sender, EventArgs e)
        {
            if (_vm.Developer.IsRegistered)
            {
                MessageBoxHelper.Show("You are already registered, please unregister first, if you want to create new BlackBerry ID token!", "Registration",
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var form = new CskRequestForm(null);
            form.ShowDialog(); // this should generate BlackBerry token file

            ///////////////////////////////
            // REGISTER

            if (!_vm.Developer.IsRegistered && _vm.Developer.HasBlackBerryTokenFile)
            {
                var registrationForm = new RegistrationForm();

                if (registrationForm.ShowDialog() == DialogResult.OK)
                {
                    var runner = new KeyToolGenRunner(RunnerDefaults.ToolsDirectory, registrationForm.AuthorName, registrationForm.AuthorPassword);
                    var success = runner.Execute();

                    // finally, show result message:
                    if (success && string.IsNullOrEmpty(runner.LastError))
                    {
                        if (!string.IsNullOrEmpty(runner.LastOutput))
                        {
                            MessageBoxHelper.Show(runner.LastOutput.Replace("CSK", "BB ID Token"), "Registered developer profile", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // clean-up files, in case something went wrong:
                        _vm.Developer.CleanupProfile();

                        if (!string.IsNullOrEmpty(runner.LastError))
                        {
                            MessageBoxHelper.Show(runner.LastError, "Failed to create developer profile", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            else
            {
                MessageBoxHelper.Show("Creation of BlackBerry ID token failed. Please try again.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateUI();
        }
    }
}
