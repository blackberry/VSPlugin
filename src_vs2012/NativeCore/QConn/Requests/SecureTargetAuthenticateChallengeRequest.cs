namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to initiate password and SSH-public key exchange.
    /// </summary>
    sealed class SecureTargetAuthenticateChallengeRequest : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetAuthenticateChallengeRequest()
            : base(8)
        {
        }

        protected override byte[] GetPayload()
        {
            return null;
        }
    }
}
