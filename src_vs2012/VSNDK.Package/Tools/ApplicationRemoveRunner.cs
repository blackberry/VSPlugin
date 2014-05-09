using System;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to remove installed application from the device.
    /// </summary>
    internal sealed class ApplicationRemoveRunner : ToolRunner
    {
        private string _packageID;
        private string _ip;
        private string _password;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="packageID">Identifier of the application .bar file</param>
        /// <param name="ip">Device IP</param>
        /// <param name="password">Device password</param>
        public ApplicationRemoveRunner(string workingDirectory, string packageID, string ip, string password)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(packageID))
                throw new ArgumentNullException("packageID");
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _packageID = packageID;
            _ip = ip;
            _password = password;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the package identifier of the application .bar file.
        /// </summary>
        public string PackageID
        {
            get { return _packageID; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _packageID = value;
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
        /// Gets and indication, if application was removed from the device.
        /// </summary>
        public bool RemovedSuccessfully
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-deploy.bat -uninstallApp -device ""{0}"" -password ""{1}"" -package-id ""{2}""", IP, Password, _packageID);
        }

        protected override void ConsumeResults(string output, string error)
        {
            RemovedSuccessfully = false;

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
                        RemovedSuccessfully = true;
                        break;
                    }
                }
            }
        }
    }
}
