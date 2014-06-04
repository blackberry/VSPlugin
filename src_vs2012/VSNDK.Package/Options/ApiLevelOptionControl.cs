using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    internal partial class ApiLevelOptionControl : UserControl
    {
        private readonly ApiLevelOptionViewModel _vm = new ApiLevelOptionViewModel();

        public ApiLevelOptionControl()
        {
            InitializeComponent();

            _vm.Dispatcher = EventDispatcher.From(this);
            PopulateNDKs(true);
        }

        private void PopulateNDKs(bool ignoreCurrentSelection)
        {
            var currentlySelected = ignoreCurrentSelection ? _vm.ActiveNDK : SelectedNDK;

            txtDescription.Text = string.Empty;
            cmbNDKs.Items.Clear();

            foreach (var ndk in _vm.InstalledNDKs)
            {
                cmbNDKs.Items.Add(ndk);
            }

            _vm.ActiveNDK = currentlySelected;      // here the currentlySelected will cause a match-by-folders-search
            cmbNDKs.SelectedItem = _vm.ActiveNDK;   // and _vm.ActiveNDK != currentlySelected instances
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

        public NdkInfo SelectedNDK
        {
            get { return cmbNDKs.SelectedItem as NdkInfo; }
            set { cmbNDKs.SelectedItem = value; }
        }

        #endregion

        internal void OnApply()
        {
            _vm.ActiveNDK = SelectedNDK;
        }

        public void OnClosed()
        {
            PopulateNDKs(true);
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
                        PopulateNDKs(true);
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
            var form = new InstallNdkForm(_vm);
            form.ShowDialog();
            PopulateNDKs(false);
        }

        private void bttStatus_Click(object sender, EventArgs e)
        {
            var form = new UpdateManagerStatusForm();
            form.ShowDialog();
        }
    }
}
