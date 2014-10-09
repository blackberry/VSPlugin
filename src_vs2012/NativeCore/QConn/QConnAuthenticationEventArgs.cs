using System;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Arguments passed along with QConnDoor authentication status changes.
    /// </summary>
    public sealed class QConnAuthenticationEventArgs : EventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public QConnAuthenticationEventArgs(QConnDoor service, bool isAuthenticated)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            Service = service;
            IsAuthenticated = isAuthenticated;
        }

        #region Properties

        public QConnDoor Service
        {
            get;
            private set;
        }

        public bool IsAuthenticated
        {
            get;
            private set;
        }

        #endregion
    }
}
