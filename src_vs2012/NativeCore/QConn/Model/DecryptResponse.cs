using System;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    sealed class DecryptResponse
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public DecryptResponse(ushort version, ushort code, byte[] decryptedBlob, byte[] signature)
        {
            if (decryptedBlob == null || decryptedBlob.Length == 0)
                throw new ArgumentNullException("decryptedBlob");
            if (signature == null || signature.Length == 0)
                throw new ArgumentNullException("signature");

            // prepare response:
            var buffer = new byte[8 + decryptedBlob.Length + signature.Length];
            BitHelper.LittleEndian_Set(buffer, 0, (uint)buffer.Length);
            BitHelper.LittleEndian_Set(buffer, 4, code);
            BitHelper.LittleEndian_Set(buffer, 6, version);
            Array.Copy(decryptedBlob, 0, buffer, 8, decryptedBlob.Length);
            Array.Copy(signature, 0, buffer, 8 + decryptedBlob.Length, signature.Length);

            // and store:
            Data = buffer;
            Version = version;
            Code = code;
            DecryptedBlob = decryptedBlob;
            Signature = signature;
            SessionKey = GetChallengeItem(2);
        }

        #region Properties

        public byte[] Data
        {
            get;
            private set;
        }

        public ushort Version
        {
            get;
            private set;
        }

        public ushort Code
        {
            get;
            private set;
        }

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

        public byte[] SessionKey
        {
            get;
            private set;
        }

        #endregion

        private byte[] GetChallengeItem(byte id)
        {
            int i = 0;

            while (i < DecryptedBlob.Length - 1)
            {
                byte length = DecryptedBlob[i];
                byte itemID = DecryptedBlob[i + 1];

                if (itemID == id)
                {
                    var result = new byte[length];
                    Array.Copy(DecryptedBlob, i + 2, result, 0, length);
                    return result;
                }

                i += 2;
                i += length;
            }

            return null;
        }
    }
}
