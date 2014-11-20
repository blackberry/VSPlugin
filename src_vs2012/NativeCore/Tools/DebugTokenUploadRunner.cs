using System;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to upload debug-token onto the device.
    /// </summary>
    public sealed class DebugTokenUploadRunner : BBToolRunner
    {
        private string _location;
        private string _ip;
        private string _password;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="debugTokenLocation">File name and directory of the debug-token bar file</param>
        /// <param name="ip">Device IP</param>
        /// <param name="password">Device password</param>
        public DebugTokenUploadRunner(string debugTokenLocation, string ip, string password)
        {
            if (string.IsNullOrEmpty(debugTokenLocation))
                throw new ArgumentNullException("debugTokenLocation");
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _location = debugTokenLocation;
            _ip = ip;
            _password = password;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the full name with location of the debugtoken.bar file.
        /// </summary>
        public string DebugTokenLocation
        {
            get { return _location; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _location = value;
                    UpdateArguments();
                }
            }
        }

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
        /// Gets and indication, if debug-token was uploaded on the device.
        /// </summary>
        public bool UploadedSuccessfully
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-deploy.bat -installDebugToken ""{0}"" -device ""{1}"" -password ""{2}""",
                                        System.Environment.ExpandEnvironmentVariables(DebugTokenLocation), IP, Password);
        }

        protected override void ConsumeResults(string output, string error)
        {
            UploadedSuccessfully = false;

            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // check, if there is any runtime error message:
                foreach (var line in lines)
                {
                    if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LastError = line.Substring(6).Trim();;
                        break;
                    }
                    if (string.Compare("result::success", line, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        UploadedSuccessfully = true;
                        break;
                    }
                }
            }
        }
    }
}
