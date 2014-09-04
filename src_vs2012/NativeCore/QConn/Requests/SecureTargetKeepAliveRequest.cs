namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to notify target that the secure connection is still alive and in use.
    /// </summary>
    internal class SecureTargetKeepAliveRequest : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetKeepAliveRequest()
            : base(6)
        {
        }

        protected override byte[] GetPayload()
        {
            return null;
        }
    }
}
