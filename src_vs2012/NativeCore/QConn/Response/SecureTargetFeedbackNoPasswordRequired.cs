namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Result informing that target doesn't require password.
    /// </summary>
    sealed class SecureTargetFeedbackNoPasswordRequired : SecureTargetFeedback
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedbackNoPasswordRequired(byte[] data, ushort version, ushort code, ushort feedbackCode)
            : base(data, HResult.OK, version, code, feedbackCode)
        {
        }
    }
}
