using System;
using System.Windows.Forms;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.Options
{
    public partial class BackupOptionControl : UserControl
    {
        private BackupOptionViewModel _vm = new BackupOptionViewModel();

        public BackupOptionControl()
        {
            InitializeComponent();
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
            }
        }
    }
}
