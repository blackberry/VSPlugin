using System;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to transfer SSH public key to the target.
    /// </summary>
    sealed class SecureTargetSendSshPublicKeyRequest : SecureTargetEncryptedRequest
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetSendSshPublicKeyRequest(byte[] sshKey, byte[] sessionKey)
            : base(7, sessionKey)
        {
            if (sshKey == null || sshKey.Length == 0)
                throw new ArgumentNullException("sshKey");

            // key, can not contain spaces at the end, otherwise it will be rejected:
            var key = Encoding.UTF8.GetString(sshKey).Trim();
            SshKey = Encoding.UTF8.GetBytes(key);
        }

        #region Properties

        public byte[] SshKey
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetUnencryptedPayload()
        {
            var payload = new byte[2 + SshKey.Length];

            BitHelper.BigEndian_Set(payload, 0, (ushort)SshKey.Length);
            Array.Copy(SshKey, 0, payload, 2, SshKey.Length);

            return payload;
        }
    }
}
