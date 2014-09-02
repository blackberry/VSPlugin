using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Interface used, when traversing over file system, when downloading files from the target.
    /// </summary>
    public interface IFileServiceVisitor
    {
        /// <summary>
        /// Gets the indication, if visitor wants to stop further processing.
        /// </summary>
        bool IsCancelled
        {
            get;
        }

        /// <summary>
        /// Called once, when visiting has started.
        /// </summary>
        void Begin(TargetFile descriptor);

        /// <summary>
        /// Called once, when visiting has finished.
        /// </summary>
        void End();

        /// <summary>
        /// Called once for each new file, which is supposed to be downloaded.
        /// </summary>
        void FileOpening(TargetFile file);
        /// <summary>
        /// Called each time part of the file is being downloaded.
        /// </summary>
        void FileContent(TargetFile file, byte[] data, ulong totalRead);
        /// <summary>
        /// Called once for each file, when download has been completed or cancelled.
        /// </summary>
        void FileClosing(TargetFile file);

        /// <summary>
        /// Called each time, when entering new folder.
        /// It will be followed by number of calls to file downloads.
        /// </summary>
        void DirectoryEntering(TargetFile folder);

        /// <summary>
        /// Called each time unexpected item was visited (for example, when no rights to enter or read).
        /// </summary>
        void UnknownEntering(TargetFile descriptor);
    }
}
