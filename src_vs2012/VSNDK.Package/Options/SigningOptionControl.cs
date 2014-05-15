using System;
using System.Diagnostics;
using System.IO;
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

        private void lblMore_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://www.blackberry.com/go/codesigning/");
        }

        private void bttNavigate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CertificatePath))
                return;

            DialogHelper.StartExplorerForFile(CertificatePath);
        }

        private void bttDeletePassword_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Delete stored password?", "BBID Token CSK Password", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;

            _vm.Developer.DeleteCskPassword();
            UpdateUI();
        }

        private void bttRefresh_Click(object sender, EventArgs e)
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
            if (form.ShowDialog() == DialogResult.Cancel)
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
    }
}
