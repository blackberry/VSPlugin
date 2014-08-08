using System;
using System.Security.Cryptography;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class describing encryption challenge.
    /// </summary>
    sealed class EncryptionChallenge
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public EncryptionChallenge(byte[] challenge)
        {
            if (challenge == null || challenge.Length == 0)
                throw new ArgumentNullException("challenge");

            // read header fields:
            Length = BitHelper.LittleEndian_ToUInt16(challenge, 0);
            Version = BitHelper.LittleEndian_ToUInt16(challenge, 2);
            int sourceLength = BitHelper.LittleEndian_ToUInt16(challenge, 4);
            SessionKeyLength = BitHelper.LittleEndian_ToUInt16(challenge, 6);
            SessionKeyType = BitHelper.LittleEndian_ToUInt16(challenge, 8);
            int containerLength = BitHelper.LittleEndian_ToUInt16(challenge, 10);
            ContainerPrimitive = BitHelper.LittleEndian_ToUInt16(challenge, 12);
            ContainerType = BitHelper.LittleEndian_ToUInt16(challenge, 14);
            ExpectedSignatureLength = BitHelper.LittleEndian_ToUInt16(challenge, 16);
            ExpectedSignatureType = BitHelper.LittleEndian_ToUInt16(challenge, 18);
            ContainerKeyVersion = BitHelper.LittleEndian_ToUInt32(challenge, 20);

            // and read payload blobs:
            SourceName = Encoding.UTF8.GetString(challenge, 24, sourceLength);

            EncryptedBlob = new byte[containerLength];
            Array.Copy(challenge, 24 + SourceName.Length, EncryptedBlob, 0, EncryptedBlob.Length);
        }

        #region Properties

        public int Length
        {
            get;
            private set;
        }

        public ushort Version
        {
            get;
            private set;
        }

        public string SourceName
        {
            get;
            private set;
        }

        public int SessionKeyLength
        {
            get;
            private set;
        }

        public ushort SessionKeyType
        {
            get;
            private set;
        }

        public ushort ContainerPrimitive
        {
            get;
            private set;
        }

        public ushort ContainerType
        {
            get;
            private set;
        }

        public int ExpectedSignatureLength
        {
            get;
            private set;
        }

        public int ExpectedSignatureType
        {
            get;
            private set;
        }

        public uint ContainerKeyVersion
        {
            get;
            private set;
        }

        public byte[] EncryptedBlob
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Decripts encryption-challenge data and provides signatures for response.
        /// </summary>
        public DecryptedChallenge Decrypt(RSAParameters publicKey, RSAParameters privateKey)
        {
            byte[] QCONNDOOR_PERMISSIONS = { 3, 4, 0x76, 0x83, 1 };

            // decrypt message:
            byte[] decryptedChallengeBlob;
            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(privateKey);
                decryptedChallengeBlob = rsa.Decrypt(EncryptedBlob, false);
            }

            // calculate its hash:
            var buffer = new byte[decryptedChallengeBlob.Length + QCONNDOOR_PERMISSIONS.Length];
            BitHelper.Copy(buffer, 0, decryptedChallengeBlob, QCONNDOOR_PERMISSIONS);
            decryptedChallengeBlob = buffer;

            // get signature of the hash:
            byte[] signature;

            // PH: HINT: In Java to actually pass the encryption-challenge
            // calculated hash was prefixed with some magic EMSA_SHA1_HASH and encrypted
            // using private key. However RSA in C# doesn't support private key encryption
            // and somehow just signing the hash from decrypted-blob is sufficient to pass.
            // Strange, but works as tested on PlayBook, Z10, Z30 and at least doesn't require
            // anymore fun with BigIntegers.
            using (var rsa = new RSACryptoServiceProvider(1024))
            {
                rsa.PersistKeyInCsp = false;
                rsa.ImportParameters(privateKey);

                using (var sha = new SHA1CryptoServiceProvider())
                {
                    signature = rsa.SignData(decryptedChallengeBlob, sha);
                }
            }

            if (signature.Length != ExpectedSignatureLength)
            {
                throw new Exception("Generated signature does not match expected signature length: " + signature.Length);
            }

            /*
            QTraceLog.PrintArray("blob", EncryptedBlob);
            QTraceLog.PrintArray("privateKeyModulusBytes", privateRsaKeyInfo.Modulus);
            QTraceLog.PrintArray("privateKeyExponentBytes", privateRsaKeyInfo.D);
            QTraceLog.PrintArray("publicKeyModulusBytes", publicRsaKeyInfo.Modulus);
            QTraceLog.PrintArray("publicKeyExponentBytes", publicRsaKeyInfo.Exponent);

            QTraceLog.PrintArray("decryptedChallengeBlob", decryptedChallengeBlob);
            QTraceLog.PrintArray("signature", signature);
             */

            return new DecryptedChallenge(decryptedChallengeBlob, signature);
        }
    }
}
