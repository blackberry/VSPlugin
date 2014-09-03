using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Support class to log statistics about visited files and folders.
    /// Mostly for testing scenarios.
    /// </summary>
    public sealed class LoggingVisitor : BaseVisitorMonitor, IFileServiceVisitor
    {
        #region Properties

        public bool IsCancelled
        {
            get;
            set;
        }

        public uint FilesCount
        {
            get;
            private set;
        }

        public ulong TotalSize
        {
            get;
            private set;
        }

        #endregion

        public void Begin(TargetServiceFile service, TargetFile descriptor)
        {
            ResetWait();

            QTraceLog.WriteLine("Initializing download for: {0}", descriptor.Path);
            FilesCount = 0;
            TotalSize = 0;
        }

        public void End()
        {
            QTraceLog.WriteLine("- DONE -");

            // notify about completion:
            NotifyCompleted();
        }

        public void FileOpening(TargetFile file)
        {
            QTraceLog.WriteLine("Started downloading: {0}", file.Name);
            FilesCount = FilesCount + 1;
        }

        public void FileClosing(TargetFile file)
        {
            QTraceLog.WriteLine("Completed downloading: {0}", file.Name);
        }

        public void FileContent(TargetFile file, byte[] data, ulong totalRead)
        {
            QTraceLog.WriteLine("Downloaded: {0}", data.Length);
            TotalSize = TotalSize + (uint) data.Length;
        }

        public void DirectoryEntering(TargetFile folder)
        {
            QTraceLog.WriteLine("Entering directory: {0}", folder.Name);
        }

        public void UnknownEntering(TargetFile other)
        {
            // no idea, if this is a directory or file or named-pipe or socket... we simply don't have rights to visit it... so ignore in statistics
        }
    }
}
