using System;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Interface informing about additional status changes of the visitors.
    /// </summary>
    public interface IFileServiceVisitorMonitor
    {
        /// <summary>
        /// Event triggered, when visiting finished.
        /// </summary>
        event EventHandler Completed;

        /// <summary>
        /// Waits until visiting is not fully completed.
        /// </summary>
        bool Wait();
    }
}
