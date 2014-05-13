using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    /// <summary>
    /// Internal dialog for adding and testing target device.
    /// </summary>
    internal partial class DeviceForm : Form
    {
        private DeviceInfoRunner _runner;

        public DeviceForm(string title)
        {
            InitializeComponent();

            Name = title;
            cmbType.SelectedIndex = 0;
            IsConnected = false;
        }

        #region Properties

        public DialogDeviceClass DeviceClass
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

        #endregion

        public DeviceDefinition ToDevice()
        {
            if (string.IsNullOrEmpty(DeviceIP) || string.IsNullOrEmpty(DevicePassword))
                return null;

            return new DeviceDefinition(DeviceName, DeviceIP, DevicePassword, DeviceClass == DialogDeviceClass.Simulator ? DeviceDefinitionType.Simulator : DeviceDefinitionType.Device);
        }

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

        private DialogDeviceClass GetDeviceClass(DeviceDefinitionType type, string ip)
        {
            if (type == DeviceDefinitionType.Simulator)
                return DialogDeviceClass.Simulator;

            return ip != null && ip.StartsWith("169.254.0.") ? DialogDeviceClass.UsbDevice : DialogDeviceClass.WiFiDevice;
        }

        private void ClearLog()
        {
            txtLogs.Text = string.Empty;
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
            _runner = new DeviceInfoRunner(RunnerDefaults.ToolsDirectory, DeviceIP, DevicePassword);
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
        }

        private void cmbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtIP.Enabled = DeviceClass != DialogDeviceClass.UsbDevice;

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

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
