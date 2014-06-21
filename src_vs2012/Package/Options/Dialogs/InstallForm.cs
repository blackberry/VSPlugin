using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class InstallForm : Form
    {
        #region Private Classes

        /// <summary>
        /// Class to order APIs from higher to lower numbers.
        /// </summary>
        class DescApiComparer : IComparer<ApiInfoArray>
        {
            public int Compare(ApiInfoArray x, ApiInfoArray y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return 1;
                if (y == null)
                    return -1;

                return y.Version.CompareTo(x.Version);
            }
        }

        #endregion

        private readonly ApiLevelOptionViewModel _vm;

        public InstallForm(ApiLevelOptionViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");

            InitializeComponent();
            _vm = vm;
            _vm.NdkListLoaded += NdkListLoaded;
            _vm.SimulatorListLoaded += SimulatorListLoaded;
            _vm.RuntimeListLoaded += RuntimeListLoaded;
            _vm.UpdateManager.Completed += ApiLevelChanged;

            UpdateUI();
        }


        #region Properties

        /// <summary>
        /// Gets or sets the active tab.
        /// </summary>
        public int ActiveTab
        {
            get { return tabControl.SelectedIndex; }
            set { tabControl.SelectedIndex = value; }
        }

        #endregion

        private void UpdateUI()
        {
            PopulateList(panelInstalledNDKs, _vm.InstalledNDKs, ApiLevelTarget.NDK, (info, target) => _vm.GetTask(info, target), "No installed Native SDK found", ApiLevelTask.Hide);
            PopulateList(panelInstalledSimulators, _vm.InstalledSimulators, ApiLevelTarget.Simulator, (info, target) => _vm.GetTask(info, target), "No installed simulators found", ApiLevelTask.Hide);
            PopulateList(panelInstalledRuntimes, _vm.InstalledRuntimes, ApiLevelTarget.Runtime, (info, target) => _vm.GetTask(info, target), "No installed runtimes found", ApiLevelTask.Hide);

            LoadList(panelAvailableNDKs, ApiLevelTarget.NDK, false);
            LoadList(panelAvailableSimulators, ApiLevelTarget.Simulator, false);
            LoadList(panelAvailableRuntimes, ApiLevelTarget.Runtime, false);
        }

        private void LoadList(Panel panel, ApiLevelTarget target, bool refresh)
        {
            if (_vm.Load(target, refresh))
            {
                PopulateList(panel, null, target, null, "Loading...", ApiLevelTask.Hide);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.NdkListLoaded -= NdkListLoaded;
            _vm.SimulatorListLoaded -= SimulatorListLoaded;
            _vm.RuntimeListLoaded -= RuntimeListLoaded;
            _vm.UpdateManager.Completed -= ApiLevelChanged;
            base.OnClosed(e);
        }

        private void NdkListLoaded(object sender, EventArgs eventArgs)
        {
            var suggested = (ApiInfoArray[]) _vm.RemoteNDKs.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableNDKs, suggested, ApiLevelTarget.NDK, (info, target) => _vm.GetTask(info, target), "There was an error, while loading list of Native SDKs", ApiLevelTask.Refresh);
        }

        private void SimulatorListLoaded(object sender, EventArgs e)
        {
            var suggested = (ApiInfoArray[])_vm.RemoteSimulators.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableSimulators, suggested, ApiLevelTarget.Simulator, (info, target) => _vm.GetTask(info, target), "There was an error, while loading list of Simulators", ApiLevelTask.Refresh);
        }

        private void RuntimeListLoaded(object sender, EventArgs e)
        {
            var suggested = (ApiInfoArray[])_vm.RemoteRuntimes.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableRuntimes, suggested, ApiLevelTarget.Runtime, (info, target) => _vm.GetTask(info, target), "There was an error, while loading list of Runtime Libraries", ApiLevelTask.Refresh);
        }

        private void ApiLevelChanged(object sender, EventArgs e)
        {
            _vm.Dispatcher.Invoke(UpdateUI);
        }

        private void PopulateList(Panel panel, IEnumerable<ApiInfo> items, ApiLevelTarget target, Func<ApiInfo, ApiLevelTarget, ApiLevelTask> actionEvaluation, string missingItemsMessage, ApiLevelTask missingItemsAction)
        {
            if (panel == null)
                return;

            int itemsAdded = 0;

            panel.Controls.Clear();
            if (items != null)
            {
                foreach (var definition in items)
                {
                    var item = new ListItemControl();

                    item.Width = panel.Width - SystemInformation.VerticalScrollBarWidth - 10;
                    item.Title = definition.Name;
                    item.Details = definition.Details;
                    item.Target = target;
                    item.Action = actionEvaluation != null ? actionEvaluation(definition, target) : ApiLevelTask.Hide;
                    item.Tag = definition;
                    item.ParentPanel = panel;
                    item.ExecuteAction += ItemOnExecuteAction;

                    panel.Controls.Add(item);
                    itemsAdded++;
                }
            }

            if (itemsAdded == 0)
            {
                var item = new ListItemControl();
                item.Width = panel.Width - SystemInformation.VerticalScrollBarWidth - 10;
                item.Title = missingItemsMessage;
                item.Details = string.Empty;
                item.Target = target;
                item.ParentPanel = panel;
                item.Action = missingItemsAction;
                item.ExecuteAction += ItemOnExecuteAction;

                panel.Controls.Add(item);
            }
        }

        private void ItemOnExecuteAction(object sender, EventArgs eventArgs)
        {
            var listItem = (ListItemControl) sender;
            var definition = listItem != null ? (ApiInfo) listItem.Tag : null;
            var actionTarget = listItem != null ? listItem.Target : ApiLevelTarget.NDK;
            var panel = listItem != null ? listItem.ParentPanel : null;
            var action = listItem != null ? listItem.Action : ApiLevelTask.Nothing;

            if (definition != null)
            {
                object argument;
                action = _vm.GetTask(definition, actionTarget, out argument);

                switch (action)
                {
                    case ApiLevelTask.Install:
                        var apiInfoArray = definition as ApiInfoArray;
                        if (apiInfoArray != null)
                        {
                            var form = new InstallConfirmationForm(apiInfoArray.Name, apiInfoArray.Items, actionTarget, (info, target) => _vm.CheckIfInstalled(info, target), (info, target) => _vm.IsProcessing(info, target));
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                var info = form.SelectedItem;
                                _vm.RequestInstall(info, actionTarget);

                                MessageBoxHelper.Show("Scheduled \"" + info + "\" for installation. Please be patient, this might take some time.\r\n\r\nProgress can be monitored in Status window available at Settings -> API-Level tab.",
                                                      null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        break;
                    case ApiLevelTask.InstallManually:
                        DialogHelper.StartURL((string)argument);
                        break;
                    case ApiLevelTask.AddExisting:
                        AddExistingNDK();
                        break;
                    case ApiLevelTask.Forget:
                        if (MessageBoxHelper.Show(definition.ToString(), "Remove own reference to existing NDK?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _vm.Forget(definition);
                            UpdateUI();
                        }
                        break;
                    case ApiLevelTask.Uninstall:
                        if (MessageBoxHelper.Show(definition.ToString(), "Do you want to remove this item?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _vm.RequestRemoval(definition, actionTarget);
                            UpdateUI();

                            MessageBoxHelper.Show("Scheduled \"" + definition + "\" for removal. Please be patient, this might take some time.",
                                null, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        break;
                }
            }

            switch (action)
            {
                case ApiLevelTask.Refresh:
                    LoadList(panel, actionTarget, true);
                    break;
            }
        }

        private void AddExistingNDK()
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
                        _vm.Reset(ApiLevelTarget.NDK);
                        UpdateUI();
                    }
                    else
                    {
                        MessageBoxHelper.Show("Unable to save NDK information", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
