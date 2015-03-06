using System;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner that calls ssh-keygen.exe to generate private/public pair of keys.
    /// </summary>
    public sealed class SshCreateKeyRunner : ToolRunner
    {
        private string _sshPublicKeyLocation;
        private string _sshPrivateKeyLocation;
        private uint _keySize;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sshKeyGenToolLocation">Full path to the ssh-keygen.exe</param>
        /// <param name="sshPublicKeyLocation">Location of the public SSH key (can be null)</param>
        /// <param name="sshPrivateKeyLocation">Location of the private SSH key</param>
        /// <param name="keySize">Size of the generated key in bits</param>
        public SshCreateKeyRunner(string sshKeyGenToolLocation, string sshPublicKeyLocation, string sshPrivateKeyLocation, uint keySize)
            : base(!string.IsNullOrEmpty(sshKeyGenToolLocation) ? Path.GetFileName(sshKeyGenToolLocation) : null,
                   !string.IsNullOrWhiteSpace(sshKeyGenToolLocation) ? Path.GetDirectoryName(sshKeyGenToolLocation) : null)
        {
            if (string.IsNullOrEmpty(sshKeyGenToolLocation))
                throw new ArgumentNullException("sshKeyGenToolLocation");
            if (!File.Exists(sshKeyGenToolLocation))
                throw new ArgumentException("Specified SSH key generator doesn't exist", "sshKeyGenToolLocation");
            if (string.IsNullOrEmpty(sshPrivateKeyLocation))
                throw new ArgumentNullException("sshPrivateKeyLocation");

            _sshPublicKeyLocation = sshPublicKeyLocation;
            _sshPrivateKeyLocation = sshPrivateKeyLocation;
            _keySize = keySize;

            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the location of the public SSH key.
        /// </summary>
        public string SshPublicKeyLocation
        {
            get { return _sshPublicKeyLocation; }
            set { _sshPublicKeyLocation = value; }
        }

        /// <summary>
        /// Gets or sets the location of the private SSH key.
        /// </summary>
        public string SshPrivateKeyLocation
        {
            get { return _sshPrivateKeyLocation; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _sshPrivateKeyLocation = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets or sets the generated key size (in bits).
        /// </summary>
        public uint KeySize
        {
            get { return _keySize; }
            set
            {
                if (value > 0)
                {
                    _keySize = value;
                    UpdateArguments();
                }
            }
        }

        #endregion

        private void UpdateArguments()
        {
            Arguments = string.Concat("-t rsa -N \"\" -b ", _keySize, " -f \"", _sshPrivateKeyLocation, "\"");
        }

        /// <summary>
        /// Method executed before starting the tool, to setup the state of the current runner.
        /// </summary>
        protected override void PrepareStartup()
        {
            // create directory, where the private and public keys are about to be stored
            Directory.CreateDirectory(Path.GetDirectoryName(_sshPrivateKeyLocation));
            if (!string.IsNullOrEmpty(_sshPublicKeyLocation))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_sshPublicKeyLocation));
            }

            // do default things
            base.PrepareStartup();
        }

        protected override void Cleanup()
        {
            // since by default, the public key is named <private-key>.pub, move the file to designated location:
            var generatedPublicKey = _sshPrivateKeyLocation + ".pub";
            if (string.IsNullOrEmpty(_sshPublicKeyLocation) && File.Exists(generatedPublicKey) && string.CompareOrdinal(_sshPublicKeyLocation, generatedPublicKey) != 0)
            {
                try
                {
                    File.Move(generatedPublicKey, _sshPublicKeyLocation);
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to move generated public SSH key from: \"{0}\" to \"{1}\"", generatedPublicKey, _sshPublicKeyLocation);
                }
            }

            base.Cleanup();
        }
    }
}
