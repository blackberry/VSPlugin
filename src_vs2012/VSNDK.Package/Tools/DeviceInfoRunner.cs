using System;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to get information about specified device.
    /// </summary>
    internal sealed class DeviceInfoRunner : ToolRunner
    {
        private string _ip;
        private string _password;

        /// <summary>
        /// Init consturctor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="ip">Device IP</param>
        /// <param name="password">Device password</param>
        public DeviceInfoRunner(string workingDirectory, string ip, string password)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _ip = ip;
            _password = password;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the IP of the device.
        /// </summary>
        public string IP
        {
            get { return _ip; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _ip = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or sets the device access password.
        /// </summary>
        public string Password
        {
            get { return _password; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _password = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets the info about the device.
        /// </summary>
        public DeviceInfo DeviceInfo
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Concat("/C blackberry-deploy.bat -listDeviceInfo \"", IP, "\" -password \"", Password, "\"");
        }

        protected override void ConsumeResults(string output, string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                string runtimeError;
                DeviceInfo = DeviceInfo.Parse(output, out runtimeError);

                // if during parsing found any runtime error, overwrite current state:
                if (!string.IsNullOrEmpty(runtimeError))
                {
                    LastError = runtimeError;
                }
            }
            else
            {
                DeviceInfo = null;
            }
        }
    }
}
