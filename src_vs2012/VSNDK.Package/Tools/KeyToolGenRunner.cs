using System;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to create a pair of signing keys for developer of specified name.
    /// </summary>
    internal sealed class KeyToolGenRunner : ToolRunner
    {
        private string _name;
        private string _password;
        private string _storeFileName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="name">Name of the developer</param>
        /// <param name="password">Password protection, required later to use the keys</param>
        /// <param name="storeFileName">Name of the certificate, where to store specified data; if null, 'author.p12' is used</param>
        public KeyToolGenRunner(string workingDirectory, string name, string password, string storeFileName)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _name = name;
            _password = password;
            _storeFileName = storeFileName;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the name of the developer used to generate keys.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _name = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or set access password, when using generated keys.
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
        /// Gets or sets the name of the certificate file, where to store the data.
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

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-keytool -genkeypair{0} -author ""{1}"" -storepass ""{2}""",
                                        string.IsNullOrEmpty(StoreFileName) ? string.Empty : string.Concat(" -keystore \"", StoreFileName, "\""),
                                        Name, Password);
        }

        protected override void ConsumeResults(string output, string error)
        {
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
                }
            }
        }
    }
}
