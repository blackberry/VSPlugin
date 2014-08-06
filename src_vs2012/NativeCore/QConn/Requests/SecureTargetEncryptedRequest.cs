using System;
using System.Security.Cryptography;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Base class for encrypted requests.
    /// </summary>
    abstract class SecureTargetEncryptedRequest : SecureTargetRequest
    {
        private readonly static RandomNumberGenerator Randomizer = RNGCryptoServiceProvider.Create();
        private byte[] _cachedPayload;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetEncryptedRequest(ushort code, byte[] sessionKey)
            : base(code)
        {
            if (sessionKey == null || sessionKey.Length == 0)
                throw new ArgumentNullException("sessionKey");

            SessionKey = sessionKey;
        }

        #region Properties

        public byte[] SessionKey
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetPayload()
        {
            // return cached value:
            if (_cachedPayload != null)
                return _cachedPayload;

            // encrypt payload:
            byte[] unencryptedPayload = GetUnencryptedPayload();
            if (unencryptedPayload == null)
                throw new Exception("Missing payload to encrypt");

            byte[] iv = new byte[16];
            Randomizer.GetBytes(iv);

            byte[] encryptedPayload = Encrypt(unencryptedPayload, iv);

            // and prepare the data to send:
            var result = _cachedPayload = new byte[4 + iv.Length + encryptedPayload.Length];

            BitHelper.BigEndian_Set(result, 0, (ushort)encryptedPayload.Length);
            BitHelper.BigEndian_Set(result, 2, (ushort)unencryptedPayload.Length);

            Array.Copy(iv, 0, result, 4, iv.Length);
            Array.Copy(encryptedPayload, 0, result, 4 + iv.Length, encryptedPayload.Length);
            return result;
        }

        private byte[] Encrypt(byte[] payload, byte[] iv)
        {
            using (var cipher = new RijndaelManaged())
            {
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;

                using (var encryptor = cipher.CreateEncryptor(SessionKey, iv))
                {
                    return encryptor.TransformFinalBlock(payload, 0, payload.Length);
                }
            }
        }

        /// <summary>
        /// Gets the payload that should be encrypted before send.
        /// </summary>
        /// <returns></returns>
        protected abstract byte[] GetUnencryptedPayload();
    }
}
