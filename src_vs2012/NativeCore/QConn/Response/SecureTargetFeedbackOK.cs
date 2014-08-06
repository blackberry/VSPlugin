namespace BlackBerry.NativeCore.QConn.Response
{
    sealed class SecureTargetFeedbackOK : SecureTargetFeedback
    {
        public SecureTargetFeedbackOK(byte[] data, ushort version, ushort code, ushort feedbackCode)
            : base(data, version, code, feedbackCode)
        {
        }
    }
}
