using System;
using System.Threading;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Base class for all FileService visitors that want to also support status change notifications and waiting.
    /// </summary>
    public abstract class BaseVisitorMonitor : IFileServiceVisitorMonitor, IDisposable
    {
        private AutoResetEvent _event;

        private int _lastProgress;
        private string _lastDestination;
        private string _lastRelativeName;
        private TransferOperation _lastOperation;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseVisitorMonitor()
        {
            _event = new AutoResetEvent(false);
        }

        ~BaseVisitorMonitor()
        {
            Dispose(false);
        }

        #region Properties

        public object Tag
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Resets the state. It clears the wait-event and fallbacks other fields to default values.
        /// </summary>
        protected void Reset()
        {
            if (_event == null)
                throw new ObjectDisposedException("BaseVisitorMonitor");

            _event.Reset();
            _lastProgress = -1;
            _lastDestination = null;
            _lastRelativeName = null;
            _lastOperation = TransferOperation.Unknown;

            // notify that visiting started:
            var handler = Started;
            if (handler != null)
            {
                handler(this, new VisitorEventArgs(Tag));
            }
        }

        #region IFileServiceVisitorMonitor Implementation

        public event EventHandler<VisitorEventArgs> Started;
        public event EventHandler<VisitorProgressChangedEventArgs> ProgressChanged;
        public event EventHandler<VisitorFailureEventArgs> Failed;
        public event EventHandler<VisitorEventArgs> Completed;

        /// <summary>
        /// Notifies subscribed listeners, that visitor finished its task and arms the wait-event.
        /// </summary>
        protected void NotifyCompleted()
        {
            if (_event == null)
                throw new ObjectDisposedException("BaseVisitorMonitor");

            var handler = Completed;
            if (handler != null)
                handler(this, new VisitorEventArgs(Tag));

            _event.Set();
        }

        /// <summary>
        /// Notifies that transfer operation of new item has started.
        /// </summary>
        protected void NotifyProgressNew(TargetFile source, string destination, string relativeName, TransferOperation operation)
        {
            var handler = ProgressChanged;
            if (handler != null)
            {
                _lastProgress = 0;
                _lastDestination = destination;
                _lastRelativeName = relativeName;
                _lastOperation = operation;
                handler(this, new VisitorProgressChangedEventArgs(source, destination, relativeName, 0, 0, operation, Tag));
            }
        }

        /// <summary>
        /// Notifies that transfer operation continues.
        /// </summary>
        protected void NotifyProgressChanged(TargetFile source, ulong transferred)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var handler = ProgressChanged;
            if (handler != null)
            {
                int progress = transferred >= source.Size ? 100 : (int) ((transferred * 100) / source.Size);
                if (progress != _lastProgress)
                {
                    _lastProgress = progress;
                    handler(this, new VisitorProgressChangedEventArgs(source, _lastDestination, _lastRelativeName, transferred, progress, _lastOperation, Tag));
                }
            }
        }

        /// <summary>
        /// Notifies that transfer operation has ended.
        /// </summary>
        protected void NotifyProgressDone(TargetFile source, ulong transferred)
        {
            var handler = ProgressChanged;
            if (handler != null && _lastProgress != 100)
            {
                var destination = _lastDestination;
                var relativeName = _lastRelativeName;
                var operation = _lastOperation;

                _lastProgress = -1;
                _lastDestination = null;
                _lastRelativeName = null;
                _lastOperation = TransferOperation.Unknown;

                handler(this, new VisitorProgressChangedEventArgs(source, destination, relativeName, transferred, 100, operation, Tag));
            }
        }

        /// <summary>
        /// Notifies that something has failed.
        /// </summary>
        protected void NotifyFailed(TargetFile descriptor, Exception ex, string message)
        {
            var handler = Failed;
            if (handler != null)
            {
                handler(this, new VisitorFailureEventArgs(descriptor, ex, message, Tag));
            }
        }

        /// <summary>
        /// Waits for the transfer operation to complete.
        /// </summary>
        public bool Wait()
        {
            if (_event == null)
                throw new ObjectDisposedException("BaseVisitorMonitor");

            return _event.WaitOne();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_event != null)
                {
                    _event.Dispose();
                    _event = null;
                }
            }
        }

        #endregion
    }
}
