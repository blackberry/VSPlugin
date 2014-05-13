using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    public partial class ApiLevelOptionControl : UserControl
    {
        private readonly ApiLevelViewModel _vm = new ApiLevelViewModel();

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
        }

        private void cmbNDKs_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedItem = cmbNDKs.SelectedItem as NdkInfo;

            txtDescription.Text = selectedItem != null ? selectedItem.ToLongDescription() : string.Empty;
        }
    }
}
