namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Successfull result about last request.
    /// </summary>
    sealed class SecureTargetFeedbackOK : SecureTargetFeedback
    {
        public SecureTargetFeedbackOK(byte[] data, ushort version, ushort code, ushort feedbackCode)
            : base(data, version, code, feedbackCode)
        {
        }
    }
}
