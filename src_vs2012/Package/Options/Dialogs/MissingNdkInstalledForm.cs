using System.Windows.Forms;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class MissingNdkInstalledForm : Form
    {
        private ApiLevelOptionViewModel _vm = new ApiLevelOptionViewModel();

        public MissingNdkInstalledForm()
        {
            InitializeComponent();
        }

        private void bttStatus_Click(object sender, System.EventArgs e)
        {
            var form = new UpdateManagerStatusForm(_vm);
            form.ShowDialog();
        }

        private void bttInstall_Click(object sender, System.EventArgs e)
        {
            var form = new InstallForm(_vm);
            form.ShowDialog();
        }
    }
}
