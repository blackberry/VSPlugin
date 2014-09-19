using System;
using System.IO;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Class saving the received files into local file system.
    /// </summary>
    public class LocalCopyVisitor : BaseVisitorMonitor, IFileServiceVisitor
    {
        private readonly string _outputPath;
        private string _basePath;
        private bool _singleFileDownload;
        private Stream _stream;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public LocalCopyVisitor(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException("outputPath");

            _outputPath = outputPath;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public LocalCopyVisitor(string outputPath, object tag)
            : this(outputPath)
        {
            Tag = tag;
        }

        #region IFileServiceVisitor Implementation

        public bool IsCancelled
        {
            get;
            set;
        }

        public void Begin(TargetServiceFile service, TargetFile descriptor)
        {
            Reset();
            _stream = null;

            if (descriptor == null)
                return;

            if (!descriptor.IsDirectory)
            {
                // only downloading a single file:
                _singleFileDownload = true;
                _basePath = null;
            }
            else
            {
                // copying folder, so get parent folder, to remember where is the root, to make processed paths of each received file or folder shorter
                _singleFileDownload = false;
                _basePath = PathHelper.ExtractDirectory(descriptor.Path);
            }
        }

        public void End()
        {
            Close();
            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
            if (_stream != null)
                throw new InvalidOperationException("Previous file was not correctly closed");

            // assumption is that directory entering, supposed to create whole directory structure,
            // was already called before on that visitor:

            string relativeName;
            string name;

            if (_singleFileDownload)
            {
                name = _outputPath;
                relativeName = PathHelper.ExtractName(_outputPath);
            }
            else
            {
                name = GetLocalPath(file, out relativeName);
            }

            _stream = File.Create(name, TargetServiceFile.DownloadUploadChunkSize, FileOptions.SequentialScan);
            NotifyProgressNew(file, name, relativeName, TransferOperation.Downloading);
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
            if (_stream == null)
                throw new ObjectDisposedException("LocalCopyVisitor");

            _stream.Write(data, 0, data.Length);
            NotifyProgressChanged(file, totalRead);
        }

        public void FileClosing(TargetFile file, ulong totalRead)
        {
            if (_stream == null)
                throw new ObjectDisposedException("LocalCopyVisitor");

            _stream.Close();
            _stream = null;

            NotifyProgressDone(file, totalRead);
        }

        public void DirectoryEntering(TargetFile folder)
        {
            string ignored;
            var name = GetLocalPath(folder, out ignored);
            Directory.CreateDirectory(name);
        }

        public void UnknownEntering(TargetFile descriptor)
        {
            // just create simple empty file:
            string relativeName;
            var name = GetLocalPath(descriptor, out relativeName);

            var stream = File.Create(name);
            stream.Close();

            NotifyProgressNew(descriptor, name, relativeName, TransferOperation.Downloading);
            NotifyProgressDone(descriptor, 0);
        }

        public void Failure(TargetFile descriptor, Exception ex, string message)
        {
            NotifyFailed(descriptor, ex, message);
        }

        private string GetLocalPath(TargetFile descriptor, out string name)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");

            name = descriptor.Path.StartsWith(_basePath) ? descriptor.Path.Substring(_basePath.Length + (IsPathSeparator(descriptor.Path, _basePath.Length) ? 1 : 0)) : descriptor.Path;
            name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return Path.Combine(_outputPath, name);
        }

        private bool IsPathSeparator(string path, int index)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (index < 0 || index >= path.Length)
                return false;

            return path[index] == Path.DirectorySeparatorChar || path[index] == Path.AltDirectorySeparatorChar;
        }

        #endregion

        private void Close()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }

            base.Dispose(disposing);
        }
    }
}
