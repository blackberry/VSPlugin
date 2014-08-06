using System;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Response to a initial encryption challenge request.
    /// </summary>
    sealed class SecureTargetEncryptedChallengeResponse : SecureTargetResponse
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetEncryptedChallengeResponse(byte[] data, ushort version, ushort code, byte[] challengeResponse)
            : base(data, version, code)
        {
            if (challengeResponse == null || challengeResponse.Length == 0)
                throw new ArgumentNullException("challengeResponse");

            Challenge = new EncryptionChallenge(challengeResponse);
        }

        #region Properties

        public EncryptionChallenge Challenge
        {
            get;
            private set;
        }

        #endregion
    }
}
