namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Base class for all types of feedback responses from QConnDoor service running on target.
    /// </summary>
    class SecureTargetFeedback : SecureTargetResponse
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedback(byte[] data, ushort version, ushort code, ushort feedbackCode)
            : base(data, version, code)
        {
            FeedbackCode = feedbackCode;
        }

        #region Properties

        public ushort FeedbackCode
        {
            get;
            private set;
        }

        #endregion
    }
}
