using System;
using System.IO;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class CertificateDetailsForm : Form
    {
        private readonly DeveloperDefinition _developer;
        private KeyToolInfoRunner _runner;
        private bool _shouldLoad;

        public CertificateDetailsForm(DeveloperDefinition developer)
        {
            if (developer == null)
                throw new ArgumentNullException("developer");
            _developer = developer;

            InitializeComponent();
            _shouldLoad = true;
            Path = _developer.CertificateFullPath;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // load info about the certificate:
            if (_shouldLoad && !string.IsNullOrEmpty(Path) && File.Exists(Path))
            {
                txtAlias.Text = "Loading...";

                // ask for password, if needed:
                if (!_developer.HasPassword)
                {
                    var form = new PasswordForm();

                    if (form.ShowDialog() != DialogResult.OK)
                    {
                        txtAlias.Text = string.Empty;
                        return;
                    }

                    // store given password:
                    _developer.UpdatePassword(form.Password, form.ShouldRemember);
                }

                _runner = new KeyToolInfoRunner(ConfigDefaults.ToolsDirectory, Path, _developer.CskPassword);
                _runner.Dispatcher = EventDispatcher.From(this);
                _runner.Finished += OnInfoLoaded;
                _runner.ExecuteAsync();
            }
        }

        #region Properties

        public string Path
        {
            get { return txtPath.Text; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    txtName.Text = string.Empty;
                    txtPath.Text = "Certificate is missing";
                    bttShow.Enabled = false;
                }
                else
                {
                    txtName.Text = System.IO.Path.GetFileName(value);
                    txtPath.Text = value;
                    bttShow.Enabled = true;
                }
            }
        }

        #endregion

        private void OnInfoLoaded(object sender, ToolRunnerEventArgs e)
        {
            if (e.IsSuccessfull && _runner.Info != null)
            {
                var info = _runner.Info;

                txtAlias.Text = info.Alias;
                txtIssuer.Text = info.Issuer;
                txtValidFrom.Text = info.ValidFrom.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                txtValidTo.Text = info.ValidTo.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                txtSerialNumber.Text = info.SerialNumber;
                txtSHA1.Text = info.FingerprintSHA1;
                txtMD5.Text = info.FingerprintMD5;
            }
            else
            {
                txtAlias.Text = "Unable to load info";
            }
        }

        private void bttShow_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Path))
                return;

            DialogHelper.StartExplorerForFile(Path);
        }
    }
}
