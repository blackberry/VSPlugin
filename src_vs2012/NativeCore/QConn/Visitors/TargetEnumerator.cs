using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Enumerator class that is able to access file-system on a target.
    /// Location can point to single file or folder (it will then go through whole subtree).
    /// </summary>
    public sealed class TargetEnumerator : IFileServiceEnumerator
    {
        private readonly string _path;
        private TargetServiceFile _service;

        /// <summary>
        /// Init constructor.
        /// Enumeration will take place from specified file or folder.
        /// </summary>
        public TargetEnumerator(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            _path = path;
        }

        #region IFileServiceEnumerator

        public void Begin(TargetServiceFile service)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            _service = service;
        }

        public void End()
        {
            _service = null;
        }

        public void Enumerate(IFileServiceVisitor visitor)
        {
            if (visitor == null)
                throw new ArgumentNullException("visitor");

            // load info about the initial path:
            var descriptor = _service.Stat(_path, false);
            if (descriptor == null)
            {
                descriptor = new TargetFile(_path, null);
            }

            // and enumerate all its secrets:
            PerformEnumeration(visitor, descriptor);
        }

        #endregion

        /// <summary>
        /// Method to visit the whole file system deep-down, starting at given point and read all info around including file contents.
        /// Then it's the visitors responsibility to use received data and save it anyhow (keep in memory, store to local file system or ZIP).
        /// </summary>
        private void PerformEnumeration(IFileServiceVisitor visitor, TargetFile descriptor)
        {
            try
            {
                visitor.Begin(descriptor);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed to initialize monitor");
            }

            Stack<TargetFile> items = new Stack<TargetFile>();
            items.Push(descriptor);

            while (items.Count > 0 && !visitor.IsCancelled)
            {
                try
                {
                    var item = items.Pop();

                    // do we have an access to open that item?
                    if (item.NoAccess)
                    {
                        visitor.UnknownEntering(item);
                        continue;
                    }

                    if (item.IsFile)
                    {
                        // download a file:
                        ReadFile(visitor, item);
                    }
                    else
                    {
                        if (item.IsDirectory)
                        {
                            // list items inside the directory and add to queue to visit them:
                            visitor.DirectoryEntering(item);
                            var childItems = _service.List(item);

                            // add non-files, to visit them last:
                            foreach (var child in childItems)
                            {
                                if (!child.IsFile)
                                    items.Push(child);
                            }

                            // then add files to visit them immediately:
                            foreach (var child in childItems)
                            {
                                if (child.IsFile)
                                    items.Push(child);
                            }
                        }
                        else
                        {
                            // treat all named-pipes, sockets etc as file and download any content if possible...
                            ReadFile(visitor, item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    QTraceLog.WriteException(ex, "Failure during download");
                }
            }

            visitor.End();
        }

        private void ReadFile(IFileServiceVisitor visitor, TargetFile descriptor)
        {
            try
            {
                using (var file = _service.Open(descriptor.Path, TargetFile.ModeOpenReadOnly, 0, false))
                {
                    if (file != null)
                    {
                        try
                        {
                            visitor.FileOpening(file);

                            ulong totalRead = 0ul;

                            while (!visitor.IsCancelled)
                            {
                                // read following chunks of the file
                                // (it's crucial that we use the same chunk size as during service cloning, so it will minimize the number of buffer allocations)
                                var data = _service.Read(file, totalRead, TargetServiceFile.DownloadUploadChunkSize);
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
                            visitor.FileClosing(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failure during download of: \"{0}\"", descriptor.Path);
            }
        }
    }
}
