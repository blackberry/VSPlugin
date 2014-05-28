using System;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to load info about developer's signing keys.
    /// </summary>
    internal sealed class KeyToolInfoRunner : ToolRunner
    {
        private string _password;
        private string _storeFileName;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="workingDirectory">Tools directory</param>
        /// <param name="storeFileName">Name of the certificate file</param>
        /// <param name="password">Required password to decrypt info inside the certificate file</param>
        public KeyToolInfoRunner(string workingDirectory, string storeFileName, string password)
            : base("cmd.exe", workingDirectory)
        {
            if (string.IsNullOrEmpty(storeFileName))
                throw new ArgumentNullException("storeFileName");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            _storeFileName = storeFileName;
            _password = password;
            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or set access password, used when generating certificate.
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
        /// Gets or sets the name of the certificate file, where to load info from.
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
        /// Gets the certificate info returned by the keytool.
        /// </summary>
        public CertificateInfo Info
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Format(@"/C blackberry-keytool -list -keystore ""{0}"" -storepass ""{1}"" -verbose", System.Environment.ExpandEnvironmentVariables(StoreFileName), Password);
        }

        protected override void ConsumeResults(string output, string error)
        {
            /*
Found 1 private key
Found 1 certificate:
    Alias:
	author
    Serial Number:
	aa:bb:cc:dd
    Subject Name:
	CommonName=XyXyXyXyXyXy
    Issuer Name:
	CommonName=XyXyXyXyXyXy
    Valid From:
	Thu Oct 18 22:25:41 CEST 2012
    Valid To:
	Wed Oct 13 22:25:41 CEST 2032
    Public Key:
	ECC-SECP521R1
    Signature Algorithm:
	SHA512withECDSA
    SHA1 Fingerprint:
	aa:bb:cc:dd:ee:ff:gg:hh:ii:jj:kk:ll:00:11:22:33:44:55:66:77
    MD5 Fingerprint:
	00:11:22:33:44:55:66:77:88:99:00:11:22:33:44:55
             */

            Info = null;
            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(output))
            {
                Info = CertificateInfo.Parse(output);
            }

            TraceLog.WriteLine("For certificate: {0}", StoreFileName);
            TraceLog.WriteLine(" * issuer: {0}", Info != null ? Info.Issuer : "- none -");
            TraceLog.WriteLine(" * algorithm: {0}", Info != null ? Info.Algorithm : "- none -");
        }
    }
}
