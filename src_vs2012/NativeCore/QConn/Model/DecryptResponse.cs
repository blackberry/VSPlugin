using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Response for encryption challenge.
    /// </summary>
    sealed class DecryptResponse
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public DecryptResponse(byte[] decryptedBlob, byte[] signature)
        {
            if (decryptedBlob == null || decryptedBlob.Length == 0)
                throw new ArgumentNullException("decryptedBlob");
            if (signature == null || signature.Length == 0)
                throw new ArgumentNullException("signature");

            // and store:
            DecryptedBlob = decryptedBlob;
            Signature = signature;
            SessionKey = GetChallengeItem(2);
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

        public byte[] SessionKey
        {
            get;
            private set;
        }

        #endregion

        private byte[] GetChallengeItem(byte id)
        {
            int i = 0;

            // record schema: <length><id><payload>
            //  - length  - byte
            //  - id      - byte
            //  - payload - length-bytes

            // iterate though all records to find one with matching ID:
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
