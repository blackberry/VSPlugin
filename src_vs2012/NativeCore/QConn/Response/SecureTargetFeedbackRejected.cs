namespace BlackBerry.NativeCore.QConn.Response
{
    sealed class SecureTargetFeedbackRejected : SecureTargetFeedback
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedbackRejected(byte[] data, ushort version, ushort code, ushort feedbackCode, string reason)
            : base(data, version, code, feedbackCode)
        {
            Reason = reason;
        }

        #region Properties

        public string Reason
        {
            get;
            private set;
        }

        #endregion
    }
}
