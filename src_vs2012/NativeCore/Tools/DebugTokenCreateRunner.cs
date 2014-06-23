using System;
using System.Text;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to create debug-token .bar file on disk.
    /// If a file exists at specified file location, it will be overwritten.
    /// </summary>
    public sealed class DebugTokenCreateRunner : ToolRunner
    {
        private string _location;
        private string _password;
        private ulong[] _devices;
        private string _storeFileName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="debugTokenLocation">File name and directory of the debug-token bar file</param>
        /// <param name="password">Password to the local certificate, specified when registering with the BlackBerry Signing Authority</param>
        /// <param name="devices">List of device PINs</param>
        /// <param name="storeFileName">Name of the certificate, where to developer data is stored; if null, 'author.p12' is used</param>
        public DebugTokenCreateRunner(string workingDirectory, string debugTokenLocation, string password, ulong[] devices, string storeFileName)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(debugTokenLocation))
                throw new ArgumentNullException("debugTokenLocation");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (devices == null || devices.Length == 0)
                throw new ArgumentNullException("devices");

            _location = debugTokenLocation;
            _password = password;
            _devices = devices;
            _storeFileName = storeFileName;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the full name with location of the debugtoken.bar file.
        /// It the file exists, if will be overwritten.
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
        /// Gets or sets the list of devices (PINs) the debug token is valid for.
        /// </summary>
        public ulong[] Devices
        {
            get { return _devices; }
            set
            {
                if (value != null && value.Length > 0)
                {
                    _devices = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or sets the access password to the local keystore.
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
        /// Gets or sets the name of the certificate file, where developer (publisher) data is stored.
        /// </summary>
        public string StoreFileName
        {
            get { return _storeFileName; }
            set
            {
                _storeFileName = value;
                UpdateArguments();
            }
        }

        /// <summary>
        /// Gets and indication, if the debug-token was created successfully.
        /// </summary>
        public bool CreatedSuccessfully
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            var args = new StringBuilder("/C blackberry-debugtokenrequest.bat ");

            // password:
            args.Append("-storepass \"").Append(Password).Append("\" ");

            // keystore:
            if (!string.IsNullOrEmpty(StoreFileName))
                args.Append("-keystore \"").Append(System.Environment.ExpandEnvironmentVariables(StoreFileName)).Append("\" ");


            // list of devices:
            foreach (var device in Devices)
            {
                args.Append("-devicepin \"").Append(device.ToString("X")).Append("\" ");
            }

            // path to the output .bar file:
            args.Append('"').Append(System.Environment.ExpandEnvironmentVariables(DebugTokenLocation)).Append('"');

            Arguments = args.ToString();
        }

        protected override void ConsumeResults(string output, string error)
        {
            CreatedSuccessfully = false;

            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // check, if there is any runtime error message:
                foreach (var line in lines)
                {
                    if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                    {
                        LastError = line.Substring(6).Trim();
                        break;
                    }
                    if (string.Compare("Info: Debug token created.", line, StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        CreatedSuccessfully = true;
                        break;
                    }
                }
            }
        }
    }
}
