namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Initial request.
    /// </summary>
    sealed class SecureTargetHello : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetHello()
            : base(1)
        {
        }

        protected override byte[] GetPayload()
        {
            // nothing extra to send to target
            return null;
        }
    }
}
