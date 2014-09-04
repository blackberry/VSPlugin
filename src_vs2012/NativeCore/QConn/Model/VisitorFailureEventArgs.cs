using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Arguments passed along with a failure transfer event.
    /// </summary>
    public sealed class VisitorFailureEventArgs : VisitorEventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public VisitorFailureEventArgs(TargetFile descriptor, Exception exception, string message, object tag)
            : base(tag)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            Descriptor = descriptor;
            Exception = exception;
            Message = message;
        }

        #region Properties

        public TargetFile Descriptor
        {
            get;
            private set;
        }

        public Exception Exception
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        public string UltimateMassage
        {
            get
            {
                if (Exception != null)
                    return Exception.Message;
                return Message;
            }
        }

        #endregion

        public override string ToString()
        {
            if (Exception != null)
                return Exception.Message + Environment.NewLine + Message;
            return Message;
        }
    }
}
