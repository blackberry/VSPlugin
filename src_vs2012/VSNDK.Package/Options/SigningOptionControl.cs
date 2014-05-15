using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Options.Dialogs;
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

            if (string.IsNullOrEmpty(Author))
            {
                MessageBoxHelper.Show("Unable to load info about author", "Invalid password or certificate file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void lblMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://www.blackberry.com/go/codesigning/");
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
            if (MessageBoxHelper.Show("Delete stored password?", "BBID Token CSK Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _vm.Developer.DeleteCskPassword();
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
                // unregister, this is the only place, where doing it synchronously is OK:

                // and delete all profile-related files:
                _vm.Developer.DeleteProfile();
                UpdateUI();
            }
        }

        private void bttRegister_Click(object sender, EventArgs e)
        {
            var form = new LoginForm(null, _vm.Developer);
            form.ShowDialog();

            // call the registration tool:

            // clean-up files, in case something went wrong:
            _vm.Developer.CleanupProfile();
            UpdateUI();
        }
    }
}
