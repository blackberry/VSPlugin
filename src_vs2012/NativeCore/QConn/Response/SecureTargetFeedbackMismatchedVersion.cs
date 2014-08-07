namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Result informing that target supports different version of the QConnDoor service protocol.
    /// </summary>
    sealed class SecureTargetFeedbackMismatchedVersion : SecureTargetFeedback
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedbackMismatchedVersion(byte[] data, ushort version, ushort code, ushort feedbackCode, string message)
            : base(data, version, code, feedbackCode)
        {
        }

        #region Properties

        public string Message
        {
            get;
            private set;
        }

        #endregion
    }
}
