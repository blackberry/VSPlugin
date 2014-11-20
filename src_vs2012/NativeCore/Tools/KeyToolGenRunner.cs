using System;
using System.IO;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to create a pair of signing keys for developer of specified name.
    /// </summary>
    public sealed class KeyToolGenRunner : BBToolRunner
    {
        private string _name;
        private string _password;
        private string _storeFileName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="name">Name of the developer</param>
        /// <param name="password">Password protection, required later to use the keys</param>
        /// <param name="storeFileName">Name of the certificate, where to store specified data; if null, 'author.p12' is used</param>
        public KeyToolGenRunner(string name, string password, string storeFileName)
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

        /// <summary>
        /// Gets the name of the certificate file created (if needed) with info about the developer (publisher).
        /// </summary>
        public string CertificateFileName
        {
            get
            {
                string fileName = null;

                if (!string.IsNullOrEmpty(_storeFileName))
                {
                    fileName = Path.GetFileName(_storeFileName);
                }

                return string.IsNullOrEmpty(fileName) ? DeveloperDefinition.DefaultCertificateName : fileName;
            }
        }

        #endregion

        private void UpdateArguments()
        {
            if (string.IsNullOrEmpty(StoreFileName))
            {
                Arguments = string.Format(@"/C blackberry-keytool -genkeypair -author ""{0}"" -storepass ""{1}""", Name, Password);
            }
            else
            {
                Arguments = string.Format(@"/C blackberry-keytool -genkeypair -author ""{0}"" -storepass ""{1}"" -keystore ""{2}""",
                                          Name, Password, System.Environment.ExpandEnvironmentVariables(StoreFileName));
            }
        }

        protected override void ConsumeResults(string output, string error)
        {
            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(output))
            {
                LastError = ExtractErrorMessages(output);
            }
        }
    }
}
