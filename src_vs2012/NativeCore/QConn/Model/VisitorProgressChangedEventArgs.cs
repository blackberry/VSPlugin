using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Arguments passed when monitoring visitor's progress.
    /// </summary>
    public sealed class VisitorProgressChangedEventArgs : VisitorEventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public VisitorProgressChangedEventArgs(TargetFile source, string destination, string relativeDestination, ulong transferredBytes, int progress, TransferOperation operation, object tag)
            : base(tag)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentNullException("destination");

            Source = source;
            Destination = destination;
            Name = string.IsNullOrEmpty(relativeDestination) ? destination : relativeDestination;
            TransferredBytes = transferredBytes;
            Progress = progress;
            Operation = operation;
        }

        #region Properties

        public TargetFile Source
        {
            get;
            private set;
        }

        public string Destination
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public ulong TransferredBytes
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets progress indication (0-100).
        /// </summary>
        public int Progress
        {
            get;
            private set;
        }

        public TransferOperation Operation
        {
            get;
            private set;
        }

        #endregion
    }
}
