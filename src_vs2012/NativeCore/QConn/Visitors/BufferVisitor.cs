using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Class saving the files received from target only inside.
    /// </summary>
    public sealed class BufferVisitor : BaseVisitorMonitor, IFileServiceVisitor
    {
        private List<Tuple<TargetFile, byte[]>> _processedBuffers;
        private TargetFile _currentFile;
        private List<byte[]> _chunks;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BufferVisitor()
        {
            Buffers = new Tuple<TargetFile, byte[]>[0];
        }

        #region Properties

        public int Count
        {
            get { return Buffers.Length; }
        }

        public Tuple<TargetFile, byte[]>[] Buffers
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the buffer value of the first item.
        /// </summary>
        public byte[] Data
        {
            get
            {
                if (Buffers.Length > 0)
                {
                    return Buffers[0].Item2;
                }

                return null;
            }
        }

        #endregion

        #region IFileServiceVisitor Implementation

        public bool IsCancelled
        {
            get;
            set;
        }

        public void Begin(TargetFile descriptor)
        {
            ResetWait();
            _processedBuffers = new List<Tuple<TargetFile, byte[]>>();
            _chunks = new List<byte[]>();
            _currentFile = null;
        }

        public void End()
        {
            Buffers = _processedBuffers.ToArray();
            _processedBuffers = null;
            _chunks = null;
            _currentFile = null;

            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
            if (_currentFile != null)
                throw new InvalidOperationException("Previous file was not correctly closed");
            _currentFile = file;
            _chunks.Clear();
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
            if (_currentFile == null)
                throw new ObjectDisposedException("BufferVisitor");

            _chunks.Add(data);
        }

        public void FileClosing(TargetFile file)
        {
            if (_currentFile == null)
                throw new ObjectDisposedException("BufferVisitor");

            _processedBuffers.Add(new Tuple<TargetFile, byte[]>(_currentFile, BitHelper.Combine(_chunks, null, 0)));
            _chunks.Clear();
            _currentFile = null;
        }

        public void DirectoryEntering(TargetFile folder)
        {
            // don't care, do nothing
        }

        public void UnknownEntering(TargetFile descriptor)
        {
            // don't care, do nothing
        }

        #endregion

        /// <summary>
        /// Gets the content of specified file.
        /// </summary>
        public byte[] Find(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            // first try to find identical executable:
            foreach (var bufferInfo in Buffers)
            {
                if (string.CompareOrdinal(bufferInfo.Item1.Path, name) == 0)
                    return bufferInfo.Item2;
            }

            // is the name matching:
            foreach (var bufferInfo in Buffers)
            {
                if (string.CompareOrdinal(bufferInfo.Item1.Name, name) == 0)
                    return bufferInfo.Item2;
            }

            // or maybe only ends with it?
            foreach (var bufferInfo in Buffers)
            {
                if (bufferInfo.Item1.Path != null && bufferInfo.Item1.Path.EndsWith(name, StringComparison.Ordinal))
                    return bufferInfo.Item2;
            }

            return null;
        }
    }
}
