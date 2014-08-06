using System;
using System.Security.Cryptography;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    sealed class EncryptionChallenge
    {
        private readonly static byte[] EMSA_SHA1_HASH = { 48, 33, 48, 9, 6, 5, 43, 14, 3, 2, 26, 5, 0, 4, 20 };
        private readonly static byte[] QCONNDOOR_PERMISSIONS = { 3, 4, 0x76, 0x83, 1 };

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
            var sourceBuffer = new byte[sourceLength];
            Array.Copy(challenge, 24, sourceBuffer, 0, sourceBuffer.Length);
            SourceName = Encoding.UTF8.GetString(sourceBuffer);

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

        public DecryptResponse Decrypt(RSACryptoServiceProvider rsa)
        {
            if (rsa == null)
                throw new ArgumentNullException("rsa");

            // decrypt message:
            var decryptedChallengeBlob = rsa.Decrypt(EncryptedBlob, false);

            // calculate its hash:
            var buffer = new byte[decryptedChallengeBlob.Length + QCONNDOOR_PERMISSIONS.Length];
            Array.Copy(decryptedChallengeBlob, 0, buffer, 0, decryptedChallengeBlob.Length);
            Array.Copy(QCONNDOOR_PERMISSIONS, 0, buffer, decryptedChallengeBlob.Length, QCONNDOOR_PERMISSIONS.Length);
            decryptedChallengeBlob = buffer;

            byte[] decryptedChallengeHash;
            using (var hash = SHA1.Create())
            {
                decryptedChallengeHash = hash.ComputeHash(decryptedChallengeBlob);
            }

            var hashBuffer = new byte[EMSA_SHA1_HASH.Length + decryptedChallengeHash.Length];
            Array.Copy(EMSA_SHA1_HASH, 0, hashBuffer, 0, EMSA_SHA1_HASH.Length);
            Array.Copy(decryptedChallengeHash, 0, hashBuffer, EMSA_SHA1_HASH.Length, decryptedChallengeHash.Length);

            // get signature of the hash:
            var signature = rsa.Encrypt(hashBuffer, false);
            if (signature.Length != ExpectedSignatureLength)
            {
                throw new Exception("Generated signature does not match expected signature length: " + signature.Length);
            }

            return new DecryptResponse(2, 0x8008, decryptedChallengeBlob, signature);
        }
    }
}
