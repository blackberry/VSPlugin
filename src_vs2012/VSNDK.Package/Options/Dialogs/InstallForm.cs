using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
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
            PopulateList(panelInstalledNDKs, _vm.InstalledNDKs, UpdateActionTargets.NDK, (info, target) => _vm.GetAction(info, target), "No installed Native SDK found", ApiLevelActionType.Hide);
            PopulateList(panelInstalledSimulators, _vm.InstalledSimulators, UpdateActionTargets.Simulator, (info, target) => _vm.GetAction(info, target), "No installed simulators found", ApiLevelActionType.Hide);
            PopulateList(panelInstalledRuntimes, _vm.InstalledRuntimes, UpdateActionTargets.Runtime, (info, target) => _vm.GetAction(info, target), "No installed runtimes found", ApiLevelActionType.Hide);

            LoadList(panelAvailableNDKs, UpdateActionTargets.NDK, false);
            LoadList(panelAvailableSimulators, UpdateActionTargets.Simulator, false);
            LoadList(panelAvailableRuntimes, UpdateActionTargets.Runtime, false);
        }

        private void LoadList(Panel panel, UpdateActionTargets target, bool refresh)
        {
            if (_vm.Load(target, refresh))
            {
                PopulateList(panel, null, target, null, "Loading...", ApiLevelActionType.Hide);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.NdkListLoaded -= NdkListLoaded;
            _vm.SimulatorListLoaded -= SimulatorListLoaded;
            _vm.RuntimeListLoaded -= RuntimeListLoaded;
            base.OnClosed(e);
        }

        private void NdkListLoaded(object sender, EventArgs eventArgs)
        {
            var suggested = (ApiInfoArray[]) _vm.RemoteNDKs.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableNDKs, suggested, UpdateActionTargets.NDK, (info, target) => _vm.GetAction(info, target), "There was an error, while loading list of Native SDKs", ApiLevelActionType.Refresh);
        }

        private void SimulatorListLoaded(object sender, EventArgs e)
        {
            var suggested = (ApiInfoArray[])_vm.RemoteSimulators.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableSimulators, suggested, UpdateActionTargets.Simulator, (info, target) => _vm.GetAction(info, target), "There was an error, while loading list of Simulators", ApiLevelActionType.Refresh);
        }

        private void RuntimeListLoaded(object sender, EventArgs e)
        {
            var suggested = (ApiInfoArray[])_vm.RemoteRuntimes.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailableRuntimes, suggested, UpdateActionTargets.Runtime, (info, target) => _vm.GetAction(info, target), "There was an error, while loading list of Runtime Libraries", ApiLevelActionType.Refresh);
        }

        private void PopulateList(Panel panel, IEnumerable<ApiInfo> items, UpdateActionTargets target, Func<ApiInfo, UpdateActionTargets, ApiLevelActionType> actionEvaluation, string missingItemsMessage, ApiLevelActionType missingItemsAction)
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
                    item.Action = actionEvaluation != null ? actionEvaluation(definition, target) : ApiLevelActionType.Hide;
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
            var actionTarget = listItem != null ? listItem.Target : UpdateActionTargets.NDK;
            var panel = listItem != null ? listItem.ParentPanel : null;
            var action = listItem != null ? listItem.Action : ApiLevelActionType.Nothing;

            if (definition != null)
            {
                object argument;
                action = _vm.GetAction(definition, actionTarget, out argument);

                switch (action)
                {
                    case ApiLevelActionType.Install:
                        var apiInfoArray = definition as ApiInfoArray;
                        if (apiInfoArray != null)
                        {
                            var form = new InstallConfirmationForm(apiInfoArray.Name, apiInfoArray.Items, actionTarget, (info, target) => _vm.CheckIfInstalled(info, target), (info, target) => _vm.IsProcessing(info, target));
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                var info = form.SelectedItem;
                                _vm.RequestInstall(info, actionTarget);

                                MessageBoxHelper.Show("Scheduled \"" + info + "\" for installation. Please be patient, this might take some time.",
                                                      "Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        break;
                    case ApiLevelActionType.InstallManually:
                        DialogHelper.StartURL((string)argument);
                        break;
                    case ApiLevelActionType.Forget:
                        if (MessageBoxHelper.Show(definition.ToString(), "Remove own reference to existing NDK?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _vm.Forget(definition);
                            UpdateUI();
                        }
                        break;
                    case ApiLevelActionType.Uninstall:
                        if (MessageBoxHelper.Show(definition.ToString(), "Do you want to remove this item?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            _vm.RequestRemoval(definition, actionTarget);
                            UpdateUI();

                            MessageBoxHelper.Show("Scheduled \"" + definition + "\" for removal. Please be patient, this might take some time.",
                                "Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        break;
                }
            }

            switch (action)
            {
                case ApiLevelActionType.Refresh:
                    LoadList(panel, actionTarget, true);
                    break;
            }
        }
    }
}
