namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Arguments passed along with visitor transfer completion event.
    /// </summary>
    public sealed class VisitorCompletionEventArgs : VisitorEventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public VisitorCompletionEventArgs(object tag, TransferOperation operation, bool wasCancelled, int fileCount)
            : base(tag)
        {
            Operation = operation;
            WasCancelled = wasCancelled;
            FileCount = fileCount;
        }

        #region Properties

        public TransferOperation Operation
        {
            get;
            private set;
        }

        public bool WasCancelled
        {
            get;
            private set;
        }

        public int FileCount
        {
            get;
            private set;
        }

        #endregion
    }
}
