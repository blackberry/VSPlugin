using System;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    sealed class TargetFileDescriptor : TargetFile, IDisposable
    {
        private TargetServiceFile _service;

        public TargetFileDescriptor(TargetServiceFile service, string handle, uint mode, ulong size, uint flags, string path, string originalPath)
            : base(mode, size, flags, path, originalPath)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentNullException("handle");

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

        internal void Closed()
        {
            _service = null;
            Handle = null;
        }
    }
}
