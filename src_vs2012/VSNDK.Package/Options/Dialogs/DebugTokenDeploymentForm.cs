using System;
using System.IO;
using System.Windows.Forms;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class DebugTokenForm : Form
    {
        private TargetsOptionViewModel _vm;
        private DeviceDefinition _device;
        private DebugTokenInfoRunner _tokenInfoRunner;
        private DeviceInfoRunner _deviceInfoRunner;
        private DebugTokenUploadRunner _tokenUploadRunner;
        private ApplicationRemoveRunner _tokenRemoveRunner;
        private DebugTokenCreateRunner _tokenCreateRunner;
        private DebugTokenInfo _tokenInfo;
        private bool _startup;

        public DebugTokenForm()
        {
            InitializeComponent();

            ErrorText = string.Empty;
            _startup = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_startup)
            {
                _startup = false;

                // if there is no path do debug-token, ask for it:
                if (string.IsNullOrEmpty(DebugTokenPath))
                {
                    bttTokenBrowse_Click(null, EventArgs.Empty);
                }

                LoadDeviceInfo(_device, false);
            }
        }

        #region Properties

        public bool IsLoadingTokenInfo
        {
            get { return _tokenInfoRunner != null; }
        }

        public bool IsUploadingToken
        {
            get { return _tokenUploadRunner != null; }
        }

        public bool IsRemovingToken
        {
            get { return _tokenRemoveRunner != null; }
        }

        public bool IsCreatingToken
        {
            get { return _tokenCreateRunner != null; }
        }

        public bool IsLoadingDeviceInfo
        {
            get { return _deviceInfoRunner != null; }
        }

        public string DebugTokenPath
        {
            get { return txtDebugTokenPath.Text; }
            set { txtDebugTokenPath.Text = value; }
        }

        public string ErrorText
        {
            get { return lblError.Text; }
            set
            {
                lblError.Text = value;
                lblError.Visible = !string.IsNullOrEmpty(value);
            }
        }

        public bool ErrorVisible
        {
            get { return lblError.Visible; }
            set { lblError.Visible = value; }
        }

        #endregion

        /// <summary>
        /// Provides the view-model for populating data.
        /// </summary>
        internal void SetVM(TargetsOptionViewModel vm, DeviceDefinition device)
        {
            if (vm == null)
                throw new ArgumentNullException("vm");
            if (device == null)
                throw new ArgumentNullException("device");
            _vm = vm;
            _device = device;

            PopulateDevices(device);
        }

        private void PopulateDevices(DeviceDefinition select)
        {
            cmbDevices.Items.Clear();

            ComboBoxItem selectedItem = null;
            foreach (var device in _vm.Devices)
            {
                var item = new ComboBoxItem(device.ShortName);
                item.Tag = device;

                if (device == select)
                    selectedItem = item;

                cmbDevices.Items.Add(item);
            }

            cmbDevices.SelectedItem = selectedItem;
        }

        #region Logs Support Methods

        private void ClearTokenLogs()
        {
            txtDebugTokenLog.Text = string.Empty;
        }

        private void AppendTokenLog(string message)
        {
            if (InvokeRequired)
                Invoke(new Action<string>(AppendTokenLog), message);
            else
                txtDebugTokenLog.Text += message + Environment.NewLine;
        }

        private void ClearDeviceLogs()
        {
            txtDeviceLog.Text = string.Empty;
        }

        private void AppendDeviceLog(string message)
        {
            if (InvokeRequired)
                Invoke(new Action<string>(AppendDeviceLog), message);
            else
                txtDeviceLog.Text += message + Environment.NewLine;
        }

        #endregion

        #region Token Info Runner

        private void LoadTokenInfo(string fileName)
        {
            if (IsLoadingTokenInfo)
                return;

            ClearTokenLogs();
            AppendTokenLog("Loading debug token info...");
            _tokenInfo = null;
            DebugTokenPath = fileName;
            UpdateTokenStatusUI(false);

            // and load token info asynchronously:
            _tokenInfoRunner = new DebugTokenInfoRunner(RunnerDefaults.ToolsDirectory, fileName);
            _tokenInfoRunner.Dispatcher = EventDispatcher.From(this);
            _tokenInfoRunner.Finished += TokenInfoRunnerOnFinished;
            _tokenInfoRunner.ExecuteAsync();
        }

        private void TokenInfoRunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            bool success = string.IsNullOrEmpty(_tokenInfoRunner.LastError) && _tokenInfoRunner.DebugToken != null;
            AppendTokenLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            if (success)
            {
                _tokenInfo = _tokenInfoRunner.DebugToken;

                TraceLog.WriteLine("Loaded debug token: {0}", _tokenInfoRunner.DebugToken);
                AppendTokenLog("Debug token found:" + Environment.NewLine + _tokenInfoRunner.DebugToken.ToLongDescription(true));
            }
            else
            {
                TraceLog.WriteLine("Failed to load info about debug token: \"{0}\"", _tokenInfoRunner.DebugTokenLocation);
                AppendTokenLog(_tokenInfoRunner.LastError);
            }

            _tokenInfoRunner.Finished -= TokenInfoRunnerOnFinished;
            _tokenInfoRunner = null;

            UpdateTokenStatusUI(true);
            UpdateDeviceStatusUI(!IsLoadingDeviceInfo);
        }

        private void UpdateTokenStatusUI(bool completed)
        {
            bttTokenBrowse.Enabled = completed;
            bttTokenCreate.Enabled = completed;
            bttTokenRenew.Enabled = completed;

            if (!completed)
            {
                ErrorText = string.Empty;
            }
            else
            {
                UpdateErrorUI();
            }
        }

        private void UpdateErrorUI()
        {
            if (_tokenInfo == null)
            {
                ErrorText = "Missing debug token to upload";
                return;
            }

            if (_tokenInfo.ExpiryDate < DateTime.UtcNow)
            {
                ErrorText = "Debug token already expired";
                return;
            }

            var deviceInfo = _vm.GetDetails(_device);
            if (deviceInfo == null)
            {
                ErrorText = "Missing connected device";
                return;
            }

            if (deviceInfo.PIN == 0)
            {
                ErrorText = "Invalid device PIN";
                return;
            }

            if (!_tokenInfo.Contains(deviceInfo.PIN))
            {
                ErrorText = "Debug token is not created against this device, recreate it adding connected device's PIN to list of target devices";
                return;
            }
        }

        #endregion

        #region Device Info Runner

        private void LoadDeviceInfo(DeviceDefinition device, bool forceReload)
        {
            if (IsLoadingDeviceInfo)
                return;

            ClearDeviceLogs();
            _device = device;
            if (_device == null)
            {
                AppendDeviceLog("Invalid device to load info");
                return;
            }

            // load device info or get it from cache:
            var deviceInfo = _vm.GetDetails(_device);
            if (deviceInfo == null || forceReload)
            {
                AppendDeviceLog("Loading device info...");
                _vm.SetDetails(_device, null);
                UpdateDeviceStatusUI(false);

                // load info asynchronously:
                _deviceInfoRunner = new DeviceInfoRunner(RunnerDefaults.ToolsDirectory, _device.IP, _device.Password);
                _deviceInfoRunner.Dispatcher = EventDispatcher.From(this);
                _deviceInfoRunner.Finished += DeviceInfoRunnerOnFinished;
                _deviceInfoRunner.ExecuteAsync();
            }
            else
            {
                PrintDeviceInfo(deviceInfo);
                UpdateDeviceStatusUI(true);
            }
        }

        private void DeviceInfoRunnerOnFinished(object sender, ToolRunnerEventArgs toolRunnerEventArgs)
        {
            bool success = string.IsNullOrEmpty(_deviceInfoRunner.LastError) && _deviceInfoRunner.DeviceInfo != null;
            AppendDeviceLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            if (success)
            {
                TraceLog.WriteLine("Loaded info about device: {0}", _deviceInfoRunner.DeviceInfo);

                // update cache:
                var deviceInfo = _deviceInfoRunner.DeviceInfo;
                _vm.SetDetails(_device, deviceInfo);

                PrintDeviceInfo(deviceInfo);
            }
            else
            {
                TraceLog.WriteLine("Failed to load info about device: {0}", _device);
                TraceLog.WarnLine(_deviceInfoRunner.LastOutput);
                AppendDeviceLog(_deviceInfoRunner.LastError);
            }

            _deviceInfoRunner.Finished -= DeviceInfoRunnerOnFinished;
            _deviceInfoRunner = null;

            UpdateDeviceStatusUI(true);
        }

        private void PrintDeviceInfo(DeviceInfo deviceInfo)
        {
            if (deviceInfo == null)
                return;

            string debugTokenDescription = deviceInfo.DebugToken != null ? deviceInfo.DebugToken.ToLongDescription() : "Error: not found";

            AppendDeviceLog("Debug token found: " + Environment.NewLine + debugTokenDescription);
            AppendDeviceLog("Device found:" + Environment.NewLine + deviceInfo.ToLongDescription());
        }

        private void UpdateDeviceStatusUI(bool completed)
        {
            var details = _vm.GetDetails(_device);

            cmbDevices.Enabled = completed;
            bttDeviceLoad.Enabled = completed && !IsLoadingDeviceInfo;
            bttUpload.Enabled = completed && details != null && _tokenInfo != null /*&& _tokenInfo.ExpiryDate > DateTime.UtcNow && _tokenInfo.Contains(details.PIN) */;
            bttRemove.Enabled = completed && details != null && details.DebugToken != null && _tokenInfo != null && !string.IsNullOrEmpty(_tokenInfo.ID);

            if (!completed)
            {
                ErrorText = string.Empty;
            }
            else
            {
                UpdateErrorUI();
            }
        }

        #endregion

        #region Debug Token Upload Runner

        private void UploadDebugToken(string fileName)
        {
            if (IsUploadingToken)
                return;

            ClearDeviceLogs();
            if (_device == null)
            {
                AppendDeviceLog("Invalid device to upload debug token");
                return;
            }

            AppendDeviceLog("Uploading debug token to " + _device + "...");
            UpdateDeviceStatusUI(false);

            // upload token asynchronously:
            _tokenUploadRunner = new DebugTokenUploadRunner(RunnerDefaults.ToolsDirectory, fileName, _device.IP, _device.Password);
            _tokenUploadRunner.Dispatcher = EventDispatcher.From(this);
            _tokenUploadRunner.Finished += DebugTokenUploadRunnerOnFinished;
            _tokenUploadRunner.ExecuteAsync();
        }

        private void DebugTokenUploadRunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            bool success = string.IsNullOrEmpty(_tokenUploadRunner.LastError) && _tokenUploadRunner.UploadedSuccessfully;
            AppendDeviceLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            if (success)
            {
                TraceLog.WriteLine("Uploaded debug token to device: {0}", _device);
            }
            else
            {
                TraceLog.WriteLine("Failed to upload debug token to device: {0}", _device);
                TraceLog.WarnLine(_tokenUploadRunner.LastError);
                AppendDeviceLog(_tokenUploadRunner.LastError);
            }

            _tokenUploadRunner.Finished -= DeviceInfoRunnerOnFinished;
            _tokenUploadRunner = null;

            UpdateDeviceStatusUI(true);

            if (success)
            {
                // reload device info:
                LoadDeviceInfo(_device, true);
            }
        }

        #endregion

        #region Debug Token Removal Runner

        private void RemoveDebugToken()
        {
            if (IsRemovingToken)
                return;

            ClearDeviceLogs();
            if (_device == null)
            {
                AppendDeviceLog("Invalid device to remove debug token");
                return;
            }
            if (_tokenInfo == null || string.IsNullOrEmpty(_tokenInfo.ID))
            {
                AppendDeviceLog("Invalid debug token info, to remove it from device");
                return;
            }

            AppendDeviceLog("Removing debug token from " + _device + "...");
            UpdateDeviceStatusUI(false);

            // remove token asynchronously:
            _tokenRemoveRunner = new ApplicationRemoveRunner(RunnerDefaults.ToolsDirectory, _tokenInfo.ID, _device.IP, _device.Password);
            _tokenRemoveRunner.Dispatcher = EventDispatcher.From(this);
            _tokenRemoveRunner.Finished += DebugTokenRemoveRunnerOnFinished;
            _tokenRemoveRunner.ExecuteAsync();
        }

        private void DebugTokenRemoveRunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            bool success = string.IsNullOrEmpty(_tokenRemoveRunner.LastError) && _tokenRemoveRunner.RemovedSuccessfully;
            AppendDeviceLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            if (success)
            {
                TraceLog.WriteLine("Removed debug token a device: {0}", _device);
            }
            else
            {
                TraceLog.WriteLine("Failed to remove debug token to device: {0}", _device);
                TraceLog.WarnLine(_tokenRemoveRunner.LastOutput);
                TraceLog.WarnLine(_tokenRemoveRunner.LastError);
                AppendDeviceLog(_tokenRemoveRunner.LastError);
            }

            _tokenRemoveRunner.Finished -= DebugTokenRemoveRunnerOnFinished;
            _tokenRemoveRunner = null;

            UpdateDeviceStatusUI(true);

            if (success)
            {
                // reload device info:
                LoadDeviceInfo(_device, true);
            }
        }

        #endregion

        #region Debug Token Create Runner

        private void CreateDebugToken(string fileName, ulong pin)
        {
            if (IsCreatingToken)
                return;

            var devices = MergeDevices(_tokenInfo, pin);

            ClearTokenLogs();
            AppendTokenLog("Creating new debug token...");
            _tokenInfo = null;
            DebugTokenPath = fileName;
            UpdateTokenStatusUI(false);

            // and create token asynchronously:
            _tokenCreateRunner = new DebugTokenCreateRunner(RunnerDefaults.ToolsDirectory, fileName, _vm.Developer.CskPassword, devices);
            _tokenCreateRunner.Dispatcher = EventDispatcher.From(this);
            _tokenCreateRunner.Finished += TokenCreateRunnerOnFinished;
            _tokenCreateRunner.ExecuteAsync();
        }

        private static ulong[] MergeDevices(DebugTokenInfo existingToken, ulong pin)
        {
            // will be only for current device:
            if (existingToken == null || existingToken.Devices == null || existingToken.Devices.Length == 0)
                return new[] { pin };

            // already contains:
            if (existingToken.Contains(pin))
                return existingToken.Devices;

            var result = new ulong[existingToken.Devices.Length + 1];
            result[0] = pin;

            for (int i = 0; i < existingToken.Devices.Length; i++)
                result[i + 1] = existingToken.Devices[i];
            return result;
        }

        private void TokenCreateRunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            bool success = string.IsNullOrEmpty(_tokenCreateRunner.LastError) && _tokenCreateRunner.CreatedSuccessfully;
            AppendTokenLog("--- DONE with " + (success ? "success" : "failure") + Environment.NewLine);

            string path = success ? _tokenCreateRunner.DebugTokenLocation : null;

            if (success)
            {
                TraceLog.WriteLine("Created new debug token at: \"{0}\"", path);
                AppendTokenLog("Debug token created at:" + Environment.NewLine + path);
            }
            else
            {
                TraceLog.WriteLine("Failed to create debug token at: \"{0}\"", path);
                AppendTokenLog(_tokenCreateRunner.LastError);
            }

            _tokenCreateRunner.Finished -= TokenCreateRunnerOnFinished;
            _tokenCreateRunner = null;

            UpdateTokenStatusUI(true);
            UpdateDeviceStatusUI(!IsLoadingDeviceInfo);

            if (success)
            {
                // reload token info:
                LoadTokenInfo(path);
            }
        }

        #endregion

        private void bttTokenBrowse_Click(object sender, EventArgs e)
        {
            if (IsLoadingTokenInfo)
                return;

            var openFile = DialogHelper.OpenBarFile("Load Debug Token",
                                                    string.IsNullOrEmpty(DebugTokenPath) ? RunnerDefaults.DataDirectory : DebugTokenPath);

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                LoadTokenInfo(openFile.FileName);
            }
        }

        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            var item = cmbDevices.SelectedItem as ComboBoxItem;
            var device = item != null ? item.Tag as DeviceDefinition : null;

            // load and display detailed info about newly selected device:
            if (device != null)
            {
                LoadDeviceInfo(device, false);
            }
        }

        private void bttDeviceLoad_Click(object sender, EventArgs e)
        {
            // force data reload:
            LoadDeviceInfo(_device, true);
        }

        private void bttUpload_Click(object sender, EventArgs e)
        {
            UploadDebugToken(DebugTokenPath);
        }

        private void bttRemove_Click(object sender, EventArgs e)
        {
            RemoveDebugToken();
        }

        private void bttTokenRenew_Click(object sender, EventArgs e)
        {
            var details = _vm.GetDetails(_device);
            if (details == null || details.PIN == 0)
            {
                MessageBoxHelper.Show("Please connect first the device and load its properties", "Missing device PIN", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (string.IsNullOrEmpty(_vm.Developer.CskPassword))
            {
                var form = new PasswordForm();

                if (form.ShowDialog() != DialogResult.OK)
                {
                    TraceLog.WarnLine("Update of BBID Token CSK password rejected");
                    return;
                }

                // save password (for current session only for persistantly):
                _vm.Developer.UpdatePassword(form.Password, form.ShouldRemember);
            }

            // ask where to store the debug-token:
            var startupPath = string.IsNullOrEmpty(DebugTokenPath) ? RunnerDefaults.DataDirectory : Path.GetDirectoryName(DebugTokenPath);
            var saveFile = DialogHelper.SaveBarFile("Renewed debug token location", startupPath, "debugtoken-" + DateTime.Now.ToString("yyyy-MM-hh") + ".bar");

            if (saveFile.ShowDialog() == DialogResult.OK)
            {
                CreateDebugToken(saveFile.FileName, details.PIN);
            }
        }
    }
}
