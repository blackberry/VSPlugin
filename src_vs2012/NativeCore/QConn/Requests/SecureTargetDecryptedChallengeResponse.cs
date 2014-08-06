using System;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    sealed class SecureTargetDecryptedChallengeResponse : SecureTargetEncryptedRequest
    {
        public SecureTargetDecryptedChallengeResponse(byte[] decryptedBlob, byte[] signature, byte[] sessionKey)
            : base(5, sessionKey)
        {
            if (decryptedBlob == null || decryptedBlob.Length == 0)
                throw new ArgumentNullException("decryptedBlob");
            if (signature == null || signature.Length == 0)
                throw new ArgumentNullException("signature");

            DecryptedBlob = decryptedBlob;
            Signature = signature;
        }

        #region Properties

        public byte[] DecryptedBlob
        {
            get;
            private set;
        }

        public byte[] Signature
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetUnencryptedPayload()
        {
            var result = new byte[12 + DecryptedBlob.Length + Signature.Length];

            BitHelper.BigEndian_Set(result, 0, (ushort) (4 + DecryptedBlob.Length + Signature.Length));
            BitHelper.BigEndian_Set(result, 2, (ushort) DecryptedBlob.Length);
            BitHelper.BigEndian_Set(result, 4, (ushort) Signature.Length);

            Array.Copy(DecryptedBlob, 0, result, 6, DecryptedBlob.Length);
            Array.Copy(Signature, 0, result, 6 + DecryptedBlob.Length, Signature.Length);
            return result;
        }
    }
}
