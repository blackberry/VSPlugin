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
        /// Called each time, when new file is supposed to be downloaded.
        /// </summary>
        void BeginFile(TargetFile file);
        /// <summary>
        /// Called each time part of the file is being downloaded.
        /// </summary>
        void ProgressFile(TargetFile file, byte[] data, ulong totalRead);
        /// <summary>
        /// Called each time, when file download has been completed or cancelled.
        /// </summary>
        void EndFile(TargetFile file);

        /// <summary>
        /// Called each time, when entering new folder.
        /// It will be followed by number of called of file downloads.
        /// </summary>
        void EnteringDirectory(TargetFile folder);

        /// <summary>
        /// Called each time unexpected item was visited (socket, named-pipe or item without access rights).
        /// </summary>
        void EnteringOther(TargetFile other);
    }
}
