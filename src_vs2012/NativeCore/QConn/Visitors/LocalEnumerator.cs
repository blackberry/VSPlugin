using System;
using System.Collections.Generic;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Enumerator class that is able to access file-system on current desktop machine.
    /// Location can point to single file or folder (it will then go through whole subtree).
    /// </summary>
    public sealed class LocalEnumerator : BaseFileServiceEnumerator
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public LocalEnumerator(string path)
            : base(path)
        {
        }

        protected override TargetFile LoadInitialInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            try
            {
                var attributes = File.GetAttributes(path);

                // create fully-featured info about the local file or folder:
                FileSystemInfo info;
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    info = new DirectoryInfo(path);
                }
                else
                {
                    info = new FileInfo(path);
                }

                return new TargetFile(info);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Unable to access: \"{0}\"", path);

                // or a stub, in case directory doesn't exist nor we have rights to open it:
                return new TargetFile(path, System.IO.Path.GetFileName(path));
            }
        }

        protected override TargetFile[] PerformDirectoryListing(TargetFile descriptor)
        {
            // is it really an existing folder?
            if (descriptor == null || !Directory.Exists(descriptor.Path))
                return null;

            // if so, then read all file and folder names:
            var result = new List<TargetFile>();

            foreach (var file in Directory.EnumerateFiles(descriptor.Path))
            {
                result.Add(new TargetFile(new FileInfo(file)));
            }

            foreach (var directory in Directory.EnumerateDirectories(descriptor.Path))
            {
                result.Add(new TargetFile(new DirectoryInfo(directory)));
            }

            // and return, so they are 
            return result.ToArray();
        }

        protected override void PerformFileRead(IFileServiceVisitor visitor, TargetFile descriptor)
        {
            if (descriptor == null || !File.Exists(descriptor.Path))
                return;

            ulong totalRead = 0ul;
            using (var file = new FileStream(descriptor.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                try
                {
                    visitor.FileOpening(descriptor);

                    var data = new byte[TargetServiceFile.DownloadUploadChunkSize];

                    while (!visitor.IsCancelled)
                    {
                        var length = file.Read(data, 0, data.Length);
                        if (length > 0)
                        {
                            totalRead += (ulong) length;

                            // is last chunk?
                            if (length != TargetServiceFile.DownloadUploadChunkSize)
                            {
                                // because, if it is, we need to allocate smaller array matching the size
                                var lastData = new byte[length];
                                Array.Copy(data, 0, lastData, 0, length);
                                visitor.FileContent(descriptor, lastData, totalRead);
                                break;
                            }

                            visitor.FileContent(descriptor, data, totalRead);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    visitor.FileClosing(descriptor, totalRead);
                }
            }
        }
    }
}
