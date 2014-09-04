using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Arguments passed along with visitor events.
    /// </summary>
    public class VisitorEventArgs : EventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public VisitorEventArgs(object tag)
        {
            Tag = tag;
        }

        #region Properties

        public object Tag
        {
            get;
            private set;
        }

        #endregion
    }
}
