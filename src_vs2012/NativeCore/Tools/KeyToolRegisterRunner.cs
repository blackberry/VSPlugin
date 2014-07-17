using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to register Signing Authority based on CSJ files.
    /// </summary>
    public sealed class KeyToolRegisterRunner : ToolRunner
    {
        private string _pin;
        private string _password;
        private string _rdkFileName;
        private string _pbdtFileName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        public KeyToolRegisterRunner(string workingDirectory, string pin, string password, string rdkFileName, string pbdtFileName)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(pin))
                throw new ArgumentNullException("pin");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");
            if (string.IsNullOrEmpty(rdkFileName))
                throw new ArgumentNullException("rdkFileName");
            if (string.IsNullOrEmpty(pbdtFileName))
                throw new ArgumentNullException("pbdtFileName");

            _pin = pin;
            _password = password;
            _rdkFileName = rdkFileName;
            _pbdtFileName = pbdtFileName;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the PIN setup at the time CSJ files were generated. It is required to decode info form them.
        /// </summary>
        public string PIN
        {
            get { return _pin; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _pin = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or sets the access password for the generated certificate.
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
        /// Gets or sets full path to the RDK file (received from BlackBerry).
        /// </summary>
        public string RdkFileName
        {
            get { return _rdkFileName; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _rdkFileName = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or sets full path to the PBDT file (received from BlackBerry).
        /// </summary>
        public string PbdtFileName
        {
            get { return _pbdtFileName; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _pbdtFileName = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets the name of the certificate file created (if needed) with info about the developer (publisher).
        /// </summary>
        public string CertificateFileName
        {
            get { return DeveloperDefinition.DefaultCertificateName; }
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-signer.bat -register -csjpin ""{0}"" -storepass ""{1}"" ""{2}"" ""{3}""",
                                      _pin, _password, _rdkFileName, _pbdtFileName);
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
