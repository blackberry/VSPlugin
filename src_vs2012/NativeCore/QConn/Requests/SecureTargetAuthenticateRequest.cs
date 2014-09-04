using System;
using System.Security.Cryptography;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to send password to the target.
    /// </summary>
    sealed class SecureTargetAuthenticateRequest : SecureTargetEncryptedRequest
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetAuthenticateRequest(string devicePassword, uint algorithm, uint iterations, byte[] salt, byte[] challenge, byte[] sessionKey)
            : base(10, sessionKey)
        {
            if (string.IsNullOrEmpty(devicePassword))
                throw new ArgumentNullException("devicePassword");
            if (salt == null || salt.Length == 0)
                throw new ArgumentNullException("salt");

            var hashedPassword = SecurePassword(Encoding.UTF8.GetBytes(devicePassword), algorithm, iterations, salt, challenge);
            HashedPassword = Encoding.UTF8.GetBytes(BitHelper.Enconde(hashedPassword));
        }

        #region Properties

        public byte[] HashedPassword
        {
            get;
            private set;
        }

        #endregion

        protected override byte[] GetUnencryptedPayload()
        {
            var payload = new Byte[2 + HashedPassword.Length];

            BitHelper.BigEndian_Set(payload, 0, (ushort)HashedPassword.Length);
            Array.Copy(HashedPassword, 0, payload, 2, HashedPassword.Length);

            return payload;
        }

        private static byte[] SHA1(byte[] data)
        {
            using (var sha = new SHA1CryptoServiceProvider())
            {
                return sha.ComputeHash(data);
            }
        }

        private static byte[] SHA512(byte[] data)
        {
            using (var sha = new SHA512CryptoServiceProvider())
            {
                return sha.ComputeHash(data);
            }
        }

        private static byte[] SecurePassword(byte[] password, uint algorithm, uint iterations, byte[] salt, byte[] challenge)
        {
            byte[] hash;

            if (algorithm == 0)
            {
                // SHA1
                hash = SHA1(password);
            }
            else
            {
                hash = CalculateHashV2(password, algorithm, iterations, salt);
            }

            // apply challenge:
            if (challenge.Length > 0)
            {
                var buffer = new byte[challenge.Length + hash.Length];
                BitHelper.Copy(buffer, 0, challenge, hash);
                if (algorithm == 0)
                {
                    hash = SHA1(buffer);
                }
                else
                {
                    hash = CalculateHashV2(buffer, algorithm, iterations, salt);
                }
            }

            return hash;
        }

        private static byte[] CalculateHashV2(byte[] password, uint algorithm, uint iterations, byte[] salt)
        {
            uint count = 0;

            byte[] hashedData = password;
            do
            {
                var buffer = new byte[4 + salt.Length + hashedData.Length];
                BitHelper.LittleEndian_Set(buffer, 0, count);
                BitHelper.Copy(buffer, 4, salt, hashedData);

                if (algorithm == 1)
                {
                    hashedData = SHA1(buffer);
                }
                else if (algorithm == 2)
                {
                    hashedData = SHA512(buffer);
                }
                count++;
            } while (count < iterations);
            return hashedData;
        }
    }
}
