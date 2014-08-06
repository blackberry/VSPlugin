namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to start other dependant services.
    /// </summary>
    sealed class SecureTargetStartServices : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetStartServices()
            : base(13)
        {
        }

        protected override byte[] GetPayload()
        {
            return null;
        }
    }
}
