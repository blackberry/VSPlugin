using System;
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
            ResetWait();
        }

        public void End()
        {
            Close();
            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
        }

        public void FileClosing(TargetFile file)
        {
        }

        public void DirectoryEntering(TargetFile folder)
        {
        }

        public void UnknownEntering(TargetFile descriptor)
        {
            // do nothing - ignore them - as expected are files and folders from known source!
        }

        #endregion

        private void Close()
        {
            
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
