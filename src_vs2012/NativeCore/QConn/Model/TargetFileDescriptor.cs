using System;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    public sealed class TargetFileDescriptor : IDisposable
    {
        private TargetServiceFile _service;

        public TargetFileDescriptor(TargetServiceFile service, string handle, uint mode, ulong size, uint flags, string path, string originalPath)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (string.IsNullOrEmpty(handle))
                throw new ArgumentNullException("handle");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (string.IsNullOrEmpty(originalPath))
                throw new ArgumentNullException("originalPath");

            _service = service;
            Handle = handle;
            Mode = mode;
            Size = size;
            Flags = flags;
            CreationTime = DateTime.MinValue;
            Path = path;
            OriginalPath = originalPath;
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

        public uint Mode
        {
            get;
            private set;
        }

        public ulong Size
        {
            get;
            private set;
        }

        public uint Flags
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public string Path
        {
            get;
            private set;
        }

        public string OriginalPath
        {
            get;
            private set;
        }

        public bool IsClosed
        {
            get { return _service == null || string.IsNullOrEmpty(Handle); }
        }

        public bool IsDirectory
        {
            get { return (Mode & 0xF000) == 0x4000; }
        }

        #endregion

        public override string ToString()
        {
            return Path;
        }

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

        internal void Update(DateTime creationTime, uint mode, ulong size)
        {
            CreationTime = creationTime;
            Mode = mode;
            Size = size;
        }
    }
}
