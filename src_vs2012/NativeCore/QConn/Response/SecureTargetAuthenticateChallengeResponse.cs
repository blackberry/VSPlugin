using System;

namespace BlackBerry.NativeCore.QConn.Response
{
    sealed class SecureTargetAuthenticateChallengeResponse : SecureTargetResponse
    {
        public SecureTargetAuthenticateChallengeResponse(byte[] data, ushort version, ushort code, uint algorithm, uint iterations, byte[] salt, byte[] challenge)
            : base(data, version, code)
        {
            if (salt == null || salt.Length == 0)
                throw new ArgumentNullException("salt");
            Algorithm = algorithm;
            Iterations = iterations;
            Salt = salt;
            Challenge = challenge;
        }

        #region Properties

        public uint Algorithm
        {
            get;
            private set;
        }

        public uint Iterations
        {
            get;
            private set;
        }

        public byte[] Salt
        {
            get;
            private set;
        }

        public byte[] Challenge
        {
            get;
            private set;
        }

        #endregion
    }
}
