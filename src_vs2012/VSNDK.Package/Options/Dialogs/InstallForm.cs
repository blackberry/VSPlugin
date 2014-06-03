using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class InstallNdkForm : Form
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

        public InstallNdkForm(ApiLevelOptionViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");

            InitializeComponent();
            _vm = vm;
            _vm.NdkListLoaded += NdkListLoaded;

            PopulateList(panelInstalled, _vm.InstalledNDKs, info => _vm.GetActionForNDK(info), "No installed Native SDK found", ApiLevelActionType.Hide);
            LoadNDKs(false);
        }

        private void LoadNDKs(bool refresh)
        {
            if (_vm.LoadNDKs(refresh))
            {
                PopulateList(panelAvailable, null, null, "Loading...", ApiLevelActionType.Hide);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _vm.NdkListLoaded -= NdkListLoaded;
            base.OnClosed(e);
        }

        private void NdkListLoaded(object sender, EventArgs eventArgs)
        {
            var suggested = (ApiInfoArray[]) _vm.RemoteNDKs.Clone();
            Array.Sort(suggested, new DescApiComparer());

            PopulateList(panelAvailable, suggested, info => _vm.GetActionForNDK(info), "There was an error, while loading list of Native SDKs", ApiLevelActionType.Refresh);
        }

        private void PopulateList(Panel panel, IEnumerable<ApiInfo> items, Func<ApiInfo, ApiLevelActionType> actionEvaluation, string missingItemsMessage, ApiLevelActionType missingItemsAction)
        {
            int itemsAdded = 0;

            panel.Controls.Clear();
            if (items != null)
            {
                foreach (var definition in items)
                {
                    var item = new ListItemControl();

                    item.Width = panel.Width - SystemInformation.VerticalScrollBarWidth - 10;
                    item.Title = definition.Name;
                    item.Details = "Device support unknown";
                    item.Action = actionEvaluation != null ? actionEvaluation(definition) : ApiLevelActionType.Hide;
                    item.Tag = definition;
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
                item.Action = missingItemsAction;

                panel.Controls.Add(item);
            }
        }

        private void ItemOnExecuteAction(object sender, EventArgs eventArgs)
        {
            var listItem = (ListItemControl) sender;
            var definition = listItem != null ? (ApiInfo) listItem.Tag : null;

            if (definition != null)
            {
                object argument;
                var action = _vm.GetActionForNDK(definition, out argument);

                switch (action)
                {
                    case ApiLevelActionType.Install:
                        var apiInfoArray = definition as ApiInfoArray;
                        if (apiInfoArray != null)
                        {
                            var form = new InstallConfirmationForm(apiInfoArray.Name, apiInfoArray.Items, info => _vm.CheckIfNdkInstalled(info), info => _vm.IsProcessingNDK(info));
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                var info = form.SelectedItem;
                                _vm.RequestNdk(info);

                                MessageBoxHelper.Show("Scheduled \"" + info + "\" for installation. Please be patient, this might take some time.",
                                                      "Update Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        break;
                    case ApiLevelActionType.InstallManually:
                        DialogHelper.StartURL((string)argument);
                        break;
                    case ApiLevelActionType.Refresh:
                        LoadNDKs(true);
                        break;
                }
            }
        }
    }
}
