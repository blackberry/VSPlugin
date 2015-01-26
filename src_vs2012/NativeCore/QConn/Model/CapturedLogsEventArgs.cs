using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Arguments passed along with captured logs.
    /// </summary>
    public sealed class CapturedLogsEventArgs : EventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public CapturedLogsEventArgs(TargetLogEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
                throw new ArgumentNullException("entries");

            Entries = entries;
        }

        #region Properties

        public TargetLogEntry[] Entries
        {
            get;
            private set;
        }

        #endregion
    }
}
