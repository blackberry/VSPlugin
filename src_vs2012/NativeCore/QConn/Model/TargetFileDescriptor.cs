using System;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Internal class wrapping the management of the handle used by target's File-System Service.
    /// </summary>
    sealed class TargetFileDescriptor : TargetFile, IDisposable
    {
        private TargetServiceFile _service;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetFileDescriptor(TargetServiceFile service, string handle, uint mode, ulong size, uint flags, string path)
            : base(mode, size, flags, path)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            _service = service;
            Handle = handle;
        }

        ~TargetFileDescriptor()
        {
            Dispose(false);
        }

        #region Properties

        public string Handle
        {
            get;
            private set;
        }

        public bool IsClosed
        {
            get { return _service == null || string.IsNullOrEmpty(Handle); }
        }

        #endregion

        #region IDisposable Implementation

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_service != null)
            {
                // ask the parental service to release the handle (it expects the Closed() to be called during service request):
                _service.Close(this);
            }
        }

        #endregion

        /// <summary>
        /// Notifies this instance, that the handle was released and is no more valid.
        /// </summary>
        internal void Closed()
        {
            _service = null;
            Handle = null;
        }
    }
}
