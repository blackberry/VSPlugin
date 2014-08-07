namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to start other dependant services.
    /// </summary>
    sealed class SecureTargetStartServicesRequest : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetStartServicesRequest()
            : base(11)
        {
        }

        protected override byte[] GetPayload()
        {
            return null;
        }
    }
}
