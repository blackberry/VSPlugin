using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Enumerator class that is able to access file-system on a target.
    /// Location can point to single file or folder (it will then go through whole subtree).
    /// </summary>
    public sealed class TargetEnumerator : BaseFileServiceEnumerator
    {
        /// <summary>
        /// Init constructor.
        /// Enumeration will take place from specified file or folder.
        /// </summary>
        public TargetEnumerator(string path)
            : base(path)
        {
        }

        protected override TargetFile LoadInitialInfo(string path)
        {
            // load info about the initial path:
            var descriptor = Service.Stat(path, false);
            if (descriptor == null)
            {
                descriptor = new TargetFile(path, null, false);
            }

            return descriptor;
        }

        protected override void PerformFileRead(IFileServiceVisitor visitor, TargetFile descriptor)
        {
            ulong totalRead = 0ul;

            using (var file = Service.Open(descriptor.Path, TargetFile.ModeOpenReadOnly, 0, false))
            {
                if (file != null)
                {
                    try
                    {
                        visitor.FileOpening(file);

                        while (!visitor.IsCancelled)
                        {
                            // read following chunks of the file
                            // (it's crucial that we use the same chunk size as during service cloning, so it will minimize the number of buffer allocations)
                            var data = Service.Read(file, totalRead, TargetServiceFile.DownloadUploadChunkSize);
                            if (data != null && data.Length > 0)
                            {
                                totalRead += (ulong) data.Length;
                                visitor.FileContent(descriptor, data, totalRead);

                                // is last chunk?
                                if (data.Length != TargetServiceFile.DownloadUploadChunkSize)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    finally
                    {
                        visitor.FileClosing(file, totalRead);
                    }
                }
            }
        }

        protected override TargetFile[] PerformDirectoryListing(TargetFile descriptor)
        {
            return Service.List(descriptor);
        }
    }
}
