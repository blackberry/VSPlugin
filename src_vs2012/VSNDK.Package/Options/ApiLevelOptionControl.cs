using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    public partial class ApiLevelOptionControl : UserControl
    {
        private readonly ApiLevelOptionViewModel _vm = new ApiLevelOptionViewModel();

        public ApiLevelOptionControl()
        {
            InitializeComponent();

            PopulateNDKs();
        }

        private void PopulateNDKs()
        {
            cmbNDKs.Items.Clear();

            foreach (var ndk in _vm.InstalledNDKs)
                cmbNDKs.Items.Add(ndk);

            cmbNDKs.SelectedItem = _vm.ActiveNDK;
        }

        private void cmbNDKs_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = cmbNDKs.SelectedItem as NdkInfo;

            txtDescription.Text = selectedItem != null ? selectedItem.ToLongDescription() : string.Empty;
        }

        #region Properties

        public bool HasSelectedNDK
        {
            get { return cmbNDKs.SelectedItem != null; }
        }

        #endregion

        internal void OnApply()
        {
            _vm.ActiveNDK = cmbNDKs.SelectedItem as NdkInfo;
        }

        public void OnClosed()
        {
            PopulateNDKs();
        }

        private void bttAddLocal_Click(object sender, EventArgs e)
        {
            var form = new AddLocalNdkForm();

            if (form.ShowDialog() == DialogResult.OK)
            {
                var ndk = form.NewNdk;
                if (ndk != null)
                {
                    // save inside 'installation config' directory:
                    if (ndk.Save(RunnerDefaults.PluginInstallationConfigDirectory))
                    {
                        // reload NDKs
                        _vm.ReloadAndActivate(ndk);
                        PopulateNDKs();
                    }
                    else
                    {
                        MessageBoxHelper.Show("Unable to save NDK information", "API Level Updater", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void bttInstallNew_Click(object sender, EventArgs e)
        {
            var form = new InstallNdkForm();
            form.ShowDialog();
        }

        private void bttNewSimulator_Click(object sender, EventArgs e)
        {
            var form = new InstallSimulatorForm();
            form.ShowDialog();
        }
    }
}
