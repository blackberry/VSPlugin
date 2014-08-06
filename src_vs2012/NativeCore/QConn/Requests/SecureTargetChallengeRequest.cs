using System;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    sealed class SecureTargetChallengeRequest : SecureTargetRequest
    {
        public SecureTargetChallengeRequest(byte[] publicKey)
            : base(3)
        {
            if (publicKey == null || publicKey.Length == 0)
                throw new ArgumentNullException("publicKey");

            PublicKey = publicKey;
        }

        #region Properties

        public byte[] PublicKey
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetPayload()
        {
            var payload = new byte[2 + PublicKey.Length];

            BitHelper.BigEndian_Set(payload, 0, (ushort) PublicKey.Length);
            Array.Copy(PublicKey, 0, payload, 2, PublicKey.Length);

            return payload;
        }
    }
}
