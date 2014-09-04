using System;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Exception thrown by QConnClient, in case of any errors.
    /// </summary>
    public sealed class QConnException : Exception
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public QConnException(string message)
            : base(message)
        {
        }
    }
}
