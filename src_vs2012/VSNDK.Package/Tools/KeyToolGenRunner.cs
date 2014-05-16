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

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="name">Name of the developer</param>
        /// <param name="password">Password protection, required later to use the keys</param>
        public KeyToolGenRunner(string workingDirectory, string name, string password)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _name = name;
            _password = password;
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

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-keytool -genkeypair -author ""{1}"" -storepass ""{0}""", Name, Password);
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
