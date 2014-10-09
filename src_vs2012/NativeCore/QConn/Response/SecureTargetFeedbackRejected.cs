using System;

namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Negative result about last request.
    /// </summary>
    sealed class SecureTargetFeedbackRejected : SecureTargetFeedback
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetFeedbackRejected(byte[] data, ushort version, ushort code, ushort feedbackCode, string reason)
            : base(data, HResult.Fail, version, code, feedbackCode)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentNullException("reason");

            Reason = reason;
        }

        #region Properties

        public string Reason
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            return Reason;
        }
    }
}
