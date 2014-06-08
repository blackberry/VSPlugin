using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    /// <summary>
    /// Dialog to show and select list of available versions per particular API Level.
    /// </summary>
    internal partial class InstallConfirmationForm : Form
    {
        public InstallConfirmationForm(string nameOverride, IEnumerable<ApiInfo> items, UpdateActionTargets target, Func<ApiInfo, UpdateActionTargets, bool> installationCheckHandler, Func<ApiInfo, UpdateActionTargets, bool> processingCheckHandler)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            InitializeComponent();

            foreach (var item in items)
            {
                var i = new ListViewItem();
                i.Tag = item;
                i.Text = string.IsNullOrEmpty(nameOverride) || item.IsBeta ? CleanName(item.Name, item.Version, item.Level) : nameOverride;
                i.SubItems.Add(item.Version.ToString());

                // display status, if it's installed or is still under processing:
                if (processingCheckHandler != null && processingCheckHandler(item, target))
                {
                    i.SubItems.Add("Processing...");
                }
                else
                {
                    if (installationCheckHandler != null && installationCheckHandler(item, target))
                        i.SubItems.Add("Installed");
                }

                listView.Items.Insert(0, i);
            }
        }

        private static string CleanName(string text, Version version, Version level)
        {
            return text.Replace(version.ToString(), string.Empty).Replace(level.ToString(), string.Empty).Trim();
        }

        #region Properties

        public ApiInfo SelectedItem
        {
            get { return listView.SelectedItems.Count > 0 ? (ApiInfo) listView.SelectedItems[0].Tag : null; }
        }

        #endregion

        private void listView_SelectedIndexChanged(object sender, EventArgs e)
        {
            bttInstall.Enabled = listView.SelectedIndices.Count > 0 && listView.SelectedItems[0].SubItems.Count <= 2;
        }
    }
}
