using System.Diagnostics;
using System.Windows.Forms;
using RIM.VSNDK_Package.Options.Dialogs;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    public partial class TargetsOptionControl : UserControl
    {
        private readonly TargetsOptionViewModel _vm = new TargetsOptionViewModel();

        public TargetsOptionControl()
        {
            InitializeComponent();

            PopulateDevices();
        }

        #region Properties

        private DeviceDefinition SelectedDevice
        {
            get
            {
                if (listTargets.SelectedItems.Count != 1)
                    return null;

                return listTargets.SelectedItems[0].Tag as DeviceDefinition;
            }
        }

        #endregion

        private void PopulateDevices()
        {
            listTargets.Items.Clear();
            
            foreach (var device in _vm.Devices)
            {
                var item = new ListViewItem();
                item.Tag = device;
                item.Text = _vm.IsActive(device) ? "x" : string.Empty;
                item.SubItems.Add(device.Type == DeviceDefinitionType.Simulator ? "S" : string.Empty);
                item.SubItems.Add(device.Name);
                item.SubItems.Add(device.IP);

                listTargets.Items.Add(item);
            }

            bttDebugToken.Enabled = _vm.RealDevicesCount > 0;
        }

        private void lnkMoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://developer.blackberry.com/native/documentation/cascades/getting_started/setting_up.html");
        }

        private void listTargets_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var device = SelectedDevice;

            bttEdit.Enabled = bttRemove.Enabled = bttActivate.Enabled = device != null;

            // update the button with 'debug-token' deployment, to limit it only to valid devices:
            device = SelectedDevice ?? _vm.ActiveDevice;
            if (device != null && device.Type != DeviceDefinitionType.Device)
                device = null;
            bttDebugToken.Enabled = _vm.RealDevicesCount > 0 && device != null;
        }

        private void listTargets_DoubleClick(object sender, System.EventArgs e)
        {
            if (SelectedDevice != null)
            {
                bttEdit_Click(sender, e);
            }
        }

        private void bttAdd_Click(object sender, System.EventArgs e)
        {
            var form = new DeviceForm("Add new Target Device");

            if (form.ShowDialog() == DialogResult.OK)
            {
                _vm.Add(form.ToDevice());
                PopulateDevices();
            }
        }

        private void bttEdit_Click(object sender, System.EventArgs e)
        {
            var form = new DeviceForm("Edit Target Device");
            var device = SelectedDevice;
            form.FromDevice(device);

            if (form.ShowDialog() == DialogResult.OK)
            {
                _vm.Update(device, form.ToDevice());
                PopulateDevices();
            }
        }

        private void bttActivate_Click(object sender, System.EventArgs e)
        {
            _vm.SetActive(SelectedDevice);
            PopulateDevices();
        }

        private void bttRemove_Click(object sender, System.EventArgs e)
        {
            var device = SelectedDevice;

            if (MessageBoxHelper.Show(device.Type == DeviceDefinitionType.Device ? "Remove the device?" : "Remove the simulator?",
                                      device.ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _vm.Remove(device);
                PopulateDevices();
            }
        }

        private void bttDebugToken_Click(object sender, System.EventArgs e)
        {
            var device = SelectedDevice ?? _vm.ActiveDevice;

            if (device != null && device.Type != DeviceDefinitionType.Device)
                device = null;

            var form = new DebugTokenForm();
            form.SetVM(_vm, device);

            if (form.ShowDialog() == DialogResult.OK)
            {

            }
        }

        public void OnApply()
        {
            _vm.Apply();
        }
    }
}
