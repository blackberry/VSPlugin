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

            if (!descriptor.IsDirectory)
            {
                // setup the base path to the home folder of the single file added to the package:
                _basePath = PathHelper.ExtractDirectory(descriptor.Path);
            }
            else
            {
                // zipping whole folder, so remember where is the root, to make paths of package-items inside shorter
                _basePath = descriptor.Path;
            }
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
                throw new ObjectDisposedException("PackagingFileServiceVisitor");

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
