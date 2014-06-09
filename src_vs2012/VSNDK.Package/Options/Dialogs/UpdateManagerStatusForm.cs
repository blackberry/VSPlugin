using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class UpdateManagerStatusForm : Form
    {
        private readonly ApiLevelOptionViewModel _vm;
        private readonly IEventDispatcher _dispatcher;

        public UpdateManagerStatusForm(ApiLevelOptionViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");
            InitializeComponent();

            _vm = vm;
            UpdateUI();

            _dispatcher = _vm.Dispatcher ?? EventDispatcher.From(this);
            _vm.UpdateManager.Started += OnActionChanged;
            _vm.UpdateManager.Completed += OnActionChanged;
            _vm.UpdateManager.Log += OnLog; // this will cause the last message to be sent again...
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.UpdateManager.Started -= OnActionChanged;
            _vm.UpdateManager.Completed -= OnActionChanged;
            _vm.UpdateManager.Log -= OnLog;
            base.OnClosed(e);
        }

        private void OnActionChanged(object sender, EventArgs e)
        {
            _dispatcher.Invoke(UpdateUI);
        }

        private void OnLog(object sender, ApiLevelUpdateLogEventArgs e)
        {
            _dispatcher.Invoke(UpdateLog, e);
        }

        private void UpdateUI()
        {
            UpdateList();
            UpdateLog(_vm.UpdateManager.CurrentAction);
        }

        private void UpdateList()
        {
            listActions.Items.Clear();
            foreach (var item in _vm.UpdateManager.Actions)
            {
                listActions.Items.Add(item);
            }

            listActions_SelectedIndexChanged(null, EventArgs.Empty);
        }

        private void UpdateLog(ViewModels.UpdateManager.ActionData action)
        {
            lblActionLog1.Text = action != null ? string.Concat("Processing ", action.Name, "...") : "Nothing is currently executing";
            lblActionLog2.Text = "-";
            progressBar.Value = 0;
            toolTip.SetToolTip(progressBar, null);
            bttAbort.Enabled = action != null && action.CanAbort;
        }

        private void UpdateLog(ApiLevelUpdateLogEventArgs e)
        {
            if (e != null && !string.IsNullOrEmpty(e.Message))
            {
                lblActionLog2.Text = e.Message;
            }
            if (e != null && e.Progress >= 0)
            {
                progressBar.Value = e.Progress;
                toolTip.SetToolTip(progressBar, progressBar.Value + "%");
            }
            bttAbort.Enabled = e != null && e.CanAbort;
        }

        private void listActions_SelectedIndexChanged(object sender, EventArgs e)
        {
            bttDelete.Enabled = listActions.SelectedIndex >= 0;
        }

        private void bttAbort_Click(object sender, EventArgs e)
        {
            var action = _vm.UpdateManager.CurrentAction;
            if (action != null)
            {
                if (MessageBoxHelper.Show(action.ToString(), "Do you want to abort currently running action?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    action.Abort();
                    UpdateUI();
                }
            }
        }

        private void bttDelete_Click(object sender, EventArgs e)
        {
            var action = listActions.SelectedItem as ViewModels.UpdateManager.ActionData;

            if (action != null)
            {
                if (MessageBoxHelper.Show(action.ToString(), "Do you want to delete this step?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    action.Delete();
                    UpdateList();
                }
            }
        }
    }
}
