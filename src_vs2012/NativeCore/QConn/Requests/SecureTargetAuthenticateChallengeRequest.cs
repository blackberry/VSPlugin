namespace BlackBerry.NativeCore.QConn.Requests
{
    sealed class SecureTargetAuthenticateChallengeRequest : SecureTargetRequest
    {
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
