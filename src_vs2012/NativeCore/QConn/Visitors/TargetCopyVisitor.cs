using System;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Class saving the received files directly on target file-system.
    /// </summary>
    public sealed class TargetCopyVisitor : BaseVisitorMonitor, IFileServiceVisitor
    {
        private readonly string _outputPath;
        private string _basePath;
        private bool _singleFileDownload;
        private TargetServiceFile _service;
        private TargetFileDescriptor _currentFile;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetCopyVisitor(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException("outputPath");

            _outputPath = outputPath;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetCopyVisitor(string outputPath, object tag)
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
            if (service == null)
                throw new ArgumentNullException("service");
            _service = service;

            Reset();

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

                // make sure path ends with '\\':
                if (!string.IsNullOrEmpty(_basePath) && _basePath[_basePath.Length - 1] != Path.DirectorySeparatorChar && _basePath[_basePath.Length - 1] != Path.AltDirectorySeparatorChar)
                {
                    _basePath += Path.DirectorySeparatorChar;
                }
            }
        }

        public void End()
        {
            Close();
            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");

            string name;
            string relativeName;

            if (_singleFileDownload)
            {
                if (IsPathSeparator(_outputPath, _outputPath.Length - 1))
                {
                    name = PathHelper.MakePath(_outputPath, PathHelper.ExtractName(file.Path));
                    relativeName = _outputPath;
                }
                else
                {
                    name = _outputPath;
                    relativeName = PathHelper.ExtractName(_outputPath);
                }
            }
            else
            {
                name = GetTargetPath(file, out relativeName);
            }

            _currentFile = _service.CreateNewFile(name, TargetFile.ModePermissionDefault);
            NotifyProgressNew(file, name, relativeName, TransferOperation.Uploading);
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");
            if (_currentFile == null)
                throw new InvalidOperationException("File is already close");

            uint length = _service.Write(_currentFile, data);
            if (length != data.Length)
                throw new QConnException("Unable to write data to \"" + _currentFile.Path + "\"");

            NotifyProgressChanged(file, totalRead);
        }

        public void FileClosing(TargetFile file, ulong totalRead)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");
            if (_currentFile == null)
                throw new InvalidOperationException("File is already closed");

            _currentFile.Close();
            _currentFile = null;

            NotifyProgressDone(file, totalRead);
        }

        public void DirectoryEntering(TargetFile folder)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");

            string ignored;
            var name = GetTargetPath(folder, out ignored);

            try
            {
                _service.CreateFolder(name, TargetFile.ModePermissionDefault);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Unable to create folder: \"{0}\"", name);
            }
        }

        public void UnknownEntering(TargetFile descriptor)
        {
            // do nothing - ignore them - as expected are files and folders from known source!
        }

        public void Failure(TargetFile descriptor, Exception ex, string message)
        {
            NotifyFailed(descriptor, ex, message);
        }

        private static bool IsPathSeparator(string path, int index)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            if (index < 0 || index > path.Length)
                return false;

            return path[index] == Path.DirectorySeparatorChar || path[index] == Path.AltDirectorySeparatorChar;
        }

        private string GetTargetPath(TargetFile descriptor, out string name)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");

            name = descriptor.Path.StartsWith(_basePath) ? descriptor.Path.Substring(_basePath.Length) : descriptor.Path;
            name = name.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return PathHelper.MakePath(_outputPath, name);
        }

        #endregion

        private void Close()
        {
            if (_currentFile != null)
            {
                _currentFile.Close();
                _currentFile = null;
            }
            _service = null;
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
