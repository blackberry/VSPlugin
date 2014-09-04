using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Interface defining the protocol of enumerating file-system resources.
    ///
    /// Potential usages could be:
    ///  - enumerating target device file-system
    ///  - enumerating local files and folders
    ///  - enumerating content of the local zip-package
    /// </summary>
    public interface IFileServiceEnumerator
    {
        /// <summary>
        /// Called once at very beginning to setup the enumerator.
        /// It should never block.
        /// </summary>
        void Begin(TargetServiceFile service);

        /// <summary>
        /// Called once at the end to clean-up used resources.
        /// </summary>
        void End();

        /// <summary>
        /// Called in dedicated thread, so it might take as much time as needed.
        /// It should enumerate all known items calling proper method of the specified visitor.
        /// </summary>
        void Enumerate(IFileServiceVisitor visitor);
    }
}
