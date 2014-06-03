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
        class DescApiComparer : IComparer<NdkInfo>
        {
            public int Compare(NdkInfo x, NdkInfo y)
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

            PopulateLists();
        }

        private void PopulateLists()
        {
            panelInstalled.Controls.Clear();

            foreach (var definition in _vm.InstalledNDKs)
            {
                var item = new ListItemControl();
                object argument;

                item.Title = definition.Name;
                item.Details = "Device support unknown";
                item.Width = panelInstalled.Width - SystemInformation.VerticalScrollBarWidth - 10;
                item.Action = _vm.GetActionForNdk(definition, out argument);
                item.ActionArgument = argument;

                panelInstalled.Controls.Add(item);
            }
        }
    }
}
