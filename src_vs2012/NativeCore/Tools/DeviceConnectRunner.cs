using System;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to establish secured connection to specified device.
    /// </summary>
    public sealed class DeviceConnectRunner : ToolRunner
    {
        private string _ip;
        private string _password;
        private string _publicKeyPath;

        public event EventHandler<EventArgs> StatusChanged;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="ip">Device IP</param>
        /// <param name="password">Device password</param>
        /// <param name="publicKeyPath">Path to SSH public key to be used, to establish connection to the device</param>
        public DeviceConnectRunner(string workingDirectory, string ip, string password, string publicKeyPath)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(publicKeyPath))
                throw new ArgumentNullException("publicKeyPath");

            _ip = ip;
            _password = password;
            _publicKeyPath = publicKeyPath;
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
        /// Gets or sets the path to the SSH public key file.
        /// </summary>
        public string PublicKeyPath
        {
            get { return _publicKeyPath; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _publicKeyPath = value;
                    UpdateArguments();
                }
            }
        }

        public bool ShowConsole
        {
            get { return ShowWindow; }
            set { ShowWindow = value; }
        }

        public bool IsConnected
        {
            get;
            private set;
        }

        public bool IsConnectionFailed
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Concat("/C blackberry-connect.bat \"", IP, "\" -password \"", Password, "\" -sshPublicKey \"", PublicKeyPath, "\"");
        }

        protected override void PrepareStartup()
        {
            base.PrepareStartup();
            IsConnected = ShowWindow;
            IsConnectionFailed = false;
        }

        protected override void ProcessOutputLine(string text)
        {
            bool wasConnected = IsConnected;
            base.ProcessOutputLine(text);

            IsConnected |= text != null && text.StartsWith("Info: Successfully connected.");
            if (!wasConnected && IsConnected)
            {
                NotifyStatusChange();
            }
        }

        protected override void ProcessErrorLine(string text)
        {
            base.ProcessErrorLine(text);
            IsConnectionFailed = true;
            LastError = ExtractErrorMessages(text);
            NotifyStatusChange();
        }

        protected override void Cleanup()
        {
            bool wasConnectedOrFailed = IsConnected || IsConnectionFailed;

            IsConnected = false;
            IsConnectionFailed = false;

            if (wasConnectedOrFailed)
            {
                NotifyStatusChange();
            }
        }

        private void NotifyStatusChange()
        {
            var eventHandler = StatusChanged;
            if (eventHandler != null)
                eventHandler(this, EventArgs.Empty);
        }
    }
}
