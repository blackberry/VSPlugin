using System;
using System.Windows.Forms;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.Dialogs
{
    internal partial class MissingNdkInstalledForm : Form
    {
        private ApiLevelOptionViewModel _vm = new ApiLevelOptionViewModel();

        public MissingNdkInstalledForm()
        {
            InitializeComponent();

            _vm.Dispatcher = EventDispatcher.From(this);
            _vm.UpdateManager.Completed += OnApiLevelChanged;

            PopulateNDKs(true);
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.ActiveNDK = SelectedNDK;
            _vm.UpdateManager.Completed -= OnApiLevelChanged;
            base.OnClosed(e);
        }

        private void OnApiLevelChanged(object sender, UpdateManagerCompletedEventArgs e)
        {
            PopulateNDKs(false);
        }

        #region Properties

        public NdkInfo SelectedNDK
        {
            get { return cmbNDKs.SelectedItem as NdkInfo; }
            set { cmbNDKs.SelectedItem = value; }
        }

        #endregion

        private void PopulateNDKs(bool ignoreCurrentSelection)
        {
            NdkInfo currentlySelected = null;

            if (ignoreCurrentSelection)
            {
                currentlySelected = _vm.ActiveNDK;
            }
            else
            {
                if (SelectedNDK == null)
                {
                    // select the last from the list, if possible:
                    SelectedNDK = _vm.GetLatestInstalledNDK();
                }
                else
                {
                    currentlySelected = SelectedNDK;
                }
            }

            // populate list of NDKs:
            cmbNDKs.Items.Clear();
            foreach (var ndk in _vm.InstalledNDKs)
            {
                cmbNDKs.Items.Add(ndk);
            }

            _vm.ActiveNDK = currentlySelected;      // here the currentlySelected will cause a match-by-folders-search
            cmbNDKs.SelectedItem = _vm.ActiveNDK;   // and _vm.ActiveNDK != currentlySelected instances
        }

        private void bttStatus_Click(object sender, EventArgs e)
        {
            var form = new UpdateManagerStatusForm(_vm);
            form.ShowDialog();
            PopulateNDKs(false);
        }

        private void bttInstall_Click(object sender, EventArgs e)
        {
            var form = new InstallForm(_vm);
            form.ShowDialog();
            PopulateNDKs(false);
        }

        private void cmbNDKs_SelectedIndexChanged(object sender, EventArgs e)
        {
            _vm.ActiveNDK = SelectedNDK;
        }
    }
}
