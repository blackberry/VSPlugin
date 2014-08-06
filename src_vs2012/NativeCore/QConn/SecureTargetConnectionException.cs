using System;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Exception thrown in case of errors, while establishing secure QConnDoor connection.
    /// </summary>
    public sealed class SecureTargetConnectionException : Exception
    {
        public SecureTargetConnectionException(HResult status, string message)
            : base(message)
        {
            Status = status;
        }

        #region Properties

        public HResult Status
        {
            get;
            private set;
        }

        #endregion
    }
}
