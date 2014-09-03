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

            ResetWait();

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

            var name = _singleFileDownload ? _outputPath : GetTargetPath(file);
            _currentFile = _service.CreateNewFile(name, TargetFile.ModePermissionDefault);
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
        }

        public void FileClosing(TargetFile file)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");
            if (_currentFile == null)
                throw new InvalidOperationException("File is already closed");

            _currentFile.Close();
            _currentFile = null;
        }

        public void DirectoryEntering(TargetFile folder)
        {
            if (_service == null)
                throw new ObjectDisposedException("TargetCopyVisitor");

            var name = GetTargetPath(folder);

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

        private string GetTargetPath(TargetFile descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");

            var name = descriptor.Path.StartsWith(_basePath) ? descriptor.Path.Substring(_basePath.Length) : descriptor.Path;
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
