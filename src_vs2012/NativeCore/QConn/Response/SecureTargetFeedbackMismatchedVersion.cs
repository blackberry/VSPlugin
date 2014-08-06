namespace BlackBerry.NativeCore.QConn.Response
{
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
