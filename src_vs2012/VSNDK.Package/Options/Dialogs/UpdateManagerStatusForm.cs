using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class UpdateManagerStatusForm : Form
    {
        private readonly ApiLevelOptionViewModel _vm;

        public UpdateManagerStatusForm(ApiLevelOptionViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");
            _vm = vm;

            InitializeComponent();

            ///_vm.UpdateManager.Log

            UpdateUI();
        }

        private void UpdateUI()
        {
            listActions.Items.Clear();
            foreach (var item in _vm.UpdateManager.Actions)
            {
                listActions.Items.Add(item);
            }

            UpdateLog(_vm.UpdateManager.CurrentAction);
        }

        private void UpdateLog(ViewModels.UpdateManager.ActionData action)
        {
            lblActionLog1.Text = action != null ? string.Concat("Processing ", action.Name, "...") : "Nothing is currently executing";
            lblActionLog2.Text = "-";
            progressBar.Value = 0;
            bttAbort.Enabled = action != null && action.CanAbort;
        }

        private void listActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            bttDelete.Enabled = listActions.SelectedIndex >= 0;
        }
    }
}
