using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.Model.Integration;

namespace BlackBerry.Package.Options.Dialogs
{
    /// <summary>
    /// Internal dialog for adding and testing target device.
    /// </summary>
    internal partial class DeviceForm : Form
    {
        enum DialogDeviceClass
        {
            WiFiDevice,
            UsbDevice,
            Simulator
        }

        private DeviceInfoRunner _runner;
        private ulong _pin;
        private string _loadedDeviceName;

        public DeviceForm(string title)
        {
            InitializeComponent();

            Text = title;
            cmbType.SelectedIndex = 0;
            IsConnected = false;
        }

        #region Properties

        private DialogDeviceClass DeviceClass
        {
            get { return (DialogDeviceClass) cmbType.SelectedIndex; }
            set { cmbType.SelectedIndex = (int) value; }
        }

        public string DeviceName
        {
            get { return txtName.Text; }
            set { txtName.Text = value; }
        }

        public string DeviceIP
        {
            get { return txtIP.Text; }
            set { txtIP.Text = value; }
        }

        public string DevicePassword
        {
            get { return txtPassword.Text; }
            set { txtPassword.Text = value; }
        }

        public bool IsConnected
        {
            get;
            private set;
        }

        public bool IsDiscoverMode
        {
            get { return cmbNames.Visible; }
        }

        public string LoadedDeviceName
        {
            get { return _loadedDeviceName; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _loadedDeviceName = null;
                    bttSetName.Text = string.Empty;
                    bttSetName.Visible = false;
                }
                else
                {
                    _loadedDeviceName = value;
                    bttSetName.Text = "<< " + (value.Length > 14 ? value.Substring(0, 14) + "..." : value); // up to 15chars
                    bttSetName.Visible = txtName.Visible;
                }
            }
        }

        private DeviceDefinition SelectedDevice
        {
            get
            {
                if (cmbNames.Enabled && cmbNames.Visible)
                {
                    var item = cmbNames.SelectedItem as ComboBoxItem;
                    var device = item != null ? item.Tag as DeviceDefinition : null;

                    return device;
                }

                return null;
            }
        }

        public ulong PIN
        {
            get { return _pin; }
            private set
            {
                _pin = value;
                txtPIN.Text = value == 0 ? string.Empty : value.ToString("X");
            }
        }

        #endregion

        public DeviceDefinition ToDevice()
        {
            if (string.IsNullOrEmpty(DeviceIP) || string.IsNullOrEmpty(DevicePassword))
                return null;

            return new DeviceDefinition(DeviceName, DeviceIP, DevicePassword, DeviceClass == DialogDeviceClass.Simulator ? DeviceDefinitionType.Simulator : DeviceDefinitionType.Device);
        }

        /// <summary>
        /// Fills the fields of the form based on the given device.
        /// </summary>
        public void FromDevice(DeviceDefinition device)
        {
            if (device == null)
            {
                DeviceClass = DialogDeviceClass.WiFiDevice;
                DeviceName = string.Empty;
                DeviceIP = string.Empty;
                DevicePassword = string.Empty;
                return;
            }

            DeviceClass = GetDeviceClass(device.Type, device.IP);
            DeviceName = device.Name;
            DeviceIP = device.IP;
            DevicePassword = device.Password;
        }

        /// <summary>
        /// Switches the default to PIN-discover mode.
        /// In this mode name field is hidden, instead the combo-box with known devices along with PIN field are shown.
        /// </summary>
        public void SetDiscoverMode(IEnumerable<DeviceDefinition> devices)
        {
            cmbType.Enabled = false;
            txtName.Visible = false;
            cmbNames.Visible = true;
            bttTest.Visible = false;
            bttDiscover.Visible = true;
            bttOK.Enabled = false;
            lblType.Visible = false;
            lblPIN.Visible = true;
            cmbType.Visible = false;
            txtPIN.Visible = true;

            cmbNames.Items.Clear();
            cmbNames.Items.Add(new ComboBoxItem(string.Empty));
            if (devices != null)
            {
                foreach (var device in devices)
                {
                    cmbNames.Items.Add(new ComboBoxItem(device.ShortName, device));
                }
            }
            cmbNames.SelectedIndex = 0;
            cmbNames.Enabled = cmbNames.Items.Count > 1;

            ActiveControl = txtIP;
        }

        private DialogDeviceClass GetDeviceClass(DeviceDefinitionType type, string ip)
        {
            if (type == DeviceDefinitionType.Simulator)
                return DialogDeviceClass.Simulator;

            return ip != null && ip.StartsWith("169.254.0.") ? DialogDeviceClass.UsbDevice : DialogDeviceClass.WiFiDevice;
        }

        private void ClearLog()
        {
            txtLogs.Text = string.Empty;
            txtLogs.SelectionLength = 0;
            txtLogs.SelectionStart = 0;
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(AppendLog), text);
            }
            else
            {
                txtLogs.Text += text + Environment.NewLine;
            }
        }

        private void bttTest_Click(object sender, EventArgs e)
        {
            ClearLog();

            // verify arguments:
            if (_runner != null)
            {
                AppendLog("Operation is still in progress. Please be patient.");
                return;
            }

            if (string.IsNullOrEmpty(DeviceIP))
            {
                AppendLog("IP address can not be empty");
                return;
            }

            if (string.IsNullOrEmpty(DevicePassword))
            {
                AppendLog("Password can not be empty");
                return;
            }

            IsConnected = false;
            LoadedDeviceName = null;
            PIN = 0;
            _runner = new DeviceInfoRunner(RunnerDefaults.ToolsDirectory, DeviceIP, DevicePassword);
            _runner.Dispatcher = EventDispatcher.From(this);
            _runner.Finished += RunnerOnFinished;

            AppendLog("Testing connection...");
            _runner.ExecuteAsync();
        }

        private void RunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            bool success = string.IsNullOrEmpty(_runner.LastError) && _runner.DeviceInfo != null;
            AppendLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            if (success)
            {
                TraceLog.WriteLine("Found device: {0} with IP: {1}", _runner.DeviceInfo.ToString(), DeviceIP);
                AppendLog("Device found:" + Environment.NewLine + _runner.DeviceInfo.ToLongDescription());
                LoadedDeviceName = _runner.DeviceInfo != null ? _runner.DeviceInfo.Name : null;
                PIN = _runner.DeviceInfo != null ? _runner.DeviceInfo.PIN : 0;
                IsConnected = true;
            }
            else
            {
                TraceLog.WriteLine("Failed to connect to: {0}", DeviceIP);
                TraceLog.WarnLine(_runner.LastOutput);
                AppendLog(_runner.LastError);
            }

            _runner.Finished -= RunnerOnFinished;
            _runner = null;

            if (IsDiscoverMode)
            {
                bttOK.Enabled = PIN > 0;
            }
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtIP.ReadOnly = DeviceClass == DialogDeviceClass.UsbDevice;

            if (DeviceClass == DialogDeviceClass.UsbDevice)
            {
                if (string.IsNullOrEmpty(DeviceName))
                    DeviceName = "usb";
                DeviceIP = "169.254.0.1";

                // got to Password control:
                ActiveControl = txtPassword;
                txtPassword.SelectionLength = 0;
                txtPassword.SelectionStart = txtPassword.Text.Length;
            }
        }

        private void bttOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(DeviceIP) || string.IsNullOrEmpty(DevicePassword))
            {
                MessageBoxHelper.Show("Sorry, some information is still missing. Please fill in all fields", "Target Device", MessageBoxIcon.Error);
                ActiveControl = string.IsNullOrEmpty(DeviceIP) ? txtIP : txtPassword;
                return;
            }

            if (IsDiscoverMode && PIN == 0)
            {
                MessageBoxHelper.Show("Sorry, no PIN found yet", "Target Device", MessageBoxIcon.Error);
                ActiveControl = bttDiscover;
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void bttSetName_Click(object sender, EventArgs e)
        {
            txtName.Text = LoadedDeviceName;
        }

        private void cmbNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = cmbNames.SelectedItem as ComboBoxItem;
            var info = item != null ? item.Tag as DeviceDefinition : null;

            if (info != null)
            {
                DeviceIP = info.IP;
                DevicePassword = info.Password;
            }
            else
            {
                DeviceIP = string.Empty;
                DevicePassword = string.Empty;
            }
        }
    }
}
