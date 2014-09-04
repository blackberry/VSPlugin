using System;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to initiate session-key and exchange of RSA public key with target.
    /// </summary>
    sealed class SecureTargetChallengeRequest : SecureTargetRequest
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetChallengeRequest(byte[] publicRsaKey)
            : base(3)
        {
            if (publicRsaKey == null || publicRsaKey.Length == 0)
                throw new ArgumentNullException("publicRsaKey");

            PublicRsaKey = publicRsaKey;
        }

        #region Properties

        public byte[] PublicRsaKey
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetPayload()
        {
            var payload = new byte[2 + PublicRsaKey.Length];

            BitHelper.BigEndian_Set(payload, 0, (ushort) PublicRsaKey.Length);
            Array.Copy(PublicRsaKey, 0, payload, 2, PublicRsaKey.Length);

            return payload;
        }
    }
}
