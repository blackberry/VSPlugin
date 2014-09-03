using System;
using System.IO;
using System.IO.Packaging;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Class packaging the files received from target into a dedicated ZIP file.
    /// </summary>
    public sealed class ZipPackageVisitor : BaseVisitorMonitor, IFileServiceVisitor
    {
        private readonly string _fileName;
        private readonly CompressionOption _compression;
        private string _basePath;
        private Package _package;
        private PackagePart _currentPart;
        private Stream _currentStream;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ZipPackageVisitor(string fileName, CompressionOption compression)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            _fileName = fileName;
            _compression = compression;
        }

        #region IFileServiceVisitor Implementation

        public bool IsCancelled
        {
            get;
            set;
        }

        public void Begin(TargetFile descriptor)
        {
            ResetWait();
            _package = Package.Open(_fileName, FileMode.Create);
            _basePath = GetInitialBasePath(descriptor);
        }

        private static string GetInitialBasePath(TargetFile descriptor)
        {
            if (!descriptor.IsDirectory)
            {
                // get the base path to the home folder of the single file processed:
                return PathHelper.ExtractDirectory(descriptor.Path);
            }

            // get whole folder, to remember where is the root, to make processed paths of each received file or folder shorter
            return descriptor.Path;
        }

        public void End()
        {
            Close();
            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
            _currentPart = CreatePart(file.Path);

            if (_currentPart == null)
                throw new InvalidOperationException("Unable to create package part to store file content");

            _currentStream = _currentPart.GetStream();
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
            if (_currentPart == null)
                throw new ObjectDisposedException("ZipPackageVisitor");

            _currentStream.Write(data, 0, data.Length);
        }

        public void FileClosing(TargetFile file)
        {
            if (_currentStream != null)
            {
                _currentStream.Close();
                _currentStream = null;
            }
            _currentPart = null;
        }

        public void DirectoryEntering(TargetFile folder)
        {
            // don't care, do nothing
        }

        public void UnknownEntering(TargetFile other)
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

            return _package.CreatePart(uri, string.Empty, _compression);
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
