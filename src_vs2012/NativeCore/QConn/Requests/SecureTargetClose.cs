namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Request to close the communication channel on target side. 
    /// </summary>
    sealed class SecureTargetClose : SecureTargetRequest
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SecureTargetClose()
            : base(12)
        {
        }

        protected override byte[] GetPayload()
        {
            // nothing extra to send to target
            return null;
        }
    }
}
