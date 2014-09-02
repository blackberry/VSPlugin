using System;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Class packaging the files received from target into a dedicated ZIP file.
    /// </summary>
    public sealed class PackagingFileServiceVisitor : IFileServiceVisitor, IFileServiceVisitorMonitor, IDisposable
    {
        private AutoResetEvent _event;
        private readonly string _fileName;
        private string _basePath;
        private Package _package;
        private PackagePart _currentPart;
        private Stream _currentStream;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public PackagingFileServiceVisitor(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            _event = new AutoResetEvent(false);
            _fileName = fileName;
        }

        ~PackagingFileServiceVisitor()
        {
            Dispose(false);
        }

        #region IFileServiceVisitor Implementation

        public bool IsCancelled
        {
            get;
            set;
        }

        public void Begin(TargetFile descriptor)
        {
            _package = Package.Open(_fileName, FileMode.Create);

            if (!descriptor.IsDirectory)
            {
                _basePath = PathHelper.ExtractDirectory(descriptor.Path);
            }
            else
            {
                _basePath = descriptor.Path;
            }
        }

        public void End()
        {
            Close();

            // notify about completion:
            var handler = Completed;
            if (handler != null)
                handler(this, EventArgs.Empty);

            _event.Set();
        }

        public void BeginFile(TargetFile file)
        {
            _currentPart = CreatePart(file.Path);

            if (_currentPart == null)
                throw new InvalidOperationException("Unable to create package part to store file content");

            _currentStream = _currentPart.GetStream();
        }

        public void ProgressFile(TargetFile file, byte[] data, ulong totalRead)
        {
            if (_currentPart == null)
                throw new ObjectDisposedException("PackagingFileServiceVisitor");

            _currentStream.Write(data, 0, data.Length);
        }

        public void EndFile(TargetFile file)
        {
            if (_currentStream != null)
            {
                _currentStream.Close();
                _currentStream = null;
            }
            _currentPart = null;
        }

        public void EnteringDirectory(TargetFile folder)
        {
        }

        public void EnteringOther(TargetFile other)
        {
            // create 0-length entry:
            CreatePart(other.Path);
        }

        private PackagePart CreatePart(string fullName)
        {
            if (_package == null)
                throw new ObjectDisposedException("PackagingFileServiceVisitor");

            var uri = PackUriHelper.CreatePartUri(GetRelativeUri(fullName));

            if (_package.PartExists(uri))
            {
                _package.DeletePart(uri);
            }

            return _package.CreatePart(uri, string.Empty, CompressionOption.Maximum);
        }

        private Uri GetRelativeUri(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                throw new ArgumentNullException("fullName");

            if (fullName.StartsWith(_basePath))
                return new Uri(fullName.Substring(_basePath.Length), UriKind.Relative);

            return new Uri(fullName, UriKind.Relative);
        }

        #endregion

        #region IFileServiceVisitorMonitor Implementation

        public event EventHandler Completed;

        public bool Wait()
        {
            if (_event == null)
                throw new ObjectDisposedException("PackagingFileSystemVisitor");

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

        private void Close()
        {
            if (_currentStream != null)
            {
                _currentStream.Close();
                _currentStream = null;
            }
            _currentPart = null;
            if (_package != null)
            {
                _package.Close();
                _package = null;
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();

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
