using System;
using System.Windows.Forms;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Options.Dialogs;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.Options
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

            // select first item:
            if (listTargets.Items.Count > 0)
                listTargets.Items[0].Selected = true;
        }

        private void lnkMoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL("http://developer.blackberry.com/native/documentation/cascades/getting_started/setting_up.html");
        }

        private void listTargets_SelectedIndexChanged(object sender, EventArgs e)
        {
            var device = SelectedDevice;

            bttEdit.Enabled = bttRemove.Enabled = bttActivate.Enabled = device != null;

            // update the button with 'debug-token' deployment, to limit it only to valid devices:
            device = SelectedDevice ?? _vm.ActiveDevice;
            if (device != null && device.Type != DeviceDefinitionType.Device)
                device = null;
            bttDebugToken.Enabled = _vm.RealDevicesCount > 0 && device != null;
        }

        private void listTargets_DoubleClick(object sender, EventArgs e)
        {
            if (SelectedDevice != null)
            {
                bttEdit_Click(sender, e);
            }
        }

        private void bttAdd_Click(object sender, EventArgs e)
        {
            var form = new DeviceForm("Add new Target Device");

            if (form.ShowDialog() == DialogResult.OK)
            {
                _vm.Add(form.ToDevice());
                PopulateDevices();
            }
        }

        private void bttEdit_Click(object sender, EventArgs e)
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

        private void bttActivate_Click(object sender, EventArgs e)
        {
            _vm.SetActive(SelectedDevice);
            PopulateDevices();
        }

        private void bttRemove_Click(object sender, EventArgs e)
        {
            var device = SelectedDevice;

            if (device != null
                && MessageBoxHelper.Show(device.Type == DeviceDefinitionType.Device ? "Remove the device?" : "Remove the simulator?",
                                      device.ToString(), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _vm.Remove(device);
                PopulateDevices();
                listTargets_SelectedIndexChanged(null, EventArgs.Empty);
            }
        }

        private void bttDebugToken_Click(object sender, EventArgs e)
        {
            var device = SelectedDevice ?? _vm.ActiveDevice;

            if (device != null && device.Type != DeviceDefinitionType.Device)
                device = null;

            if (device != null)
            {
                var form = new DebugTokenDeploymentForm(_vm, device);
                form.AskOnStartup = false;
                form.ShowDialog();

                // refresh the list, as it's possible to add new device from debug-token deployment form:
                PopulateDevices();
            }
            else
            {
                MessageBoxHelper.Show("Please select a device first", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void OnApply()
        {
            _vm.Apply();
        }

        public void OnReset()
        {
            _vm.Reset();
            PopulateDevices();
        }
    }
}
