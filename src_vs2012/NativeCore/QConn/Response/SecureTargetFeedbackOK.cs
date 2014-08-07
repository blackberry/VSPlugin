namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Successfull result about last request.
    /// </summary>
    sealed class SecureTargetFeedbackOK : SecureTargetFeedback
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedbackOK(byte[] data, ushort version, ushort code, ushort feedbackCode)
            : base(data, HResult.OK, version, code, feedbackCode)
        {
        }
    }
}
