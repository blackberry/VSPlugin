using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Visitors
{
    /// <summary>
    /// Base class for enumerators traversing the abstract file system.
    /// </summary>
    public abstract class BaseFileServiceEnumerator : IFileServiceEnumerator
    {
        private readonly string _path;
        private TargetServiceFile _service;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BaseFileServiceEnumerator(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            _path = path;
        }

        #region Properties

        /// <summary>
        /// Gets the reference to the file-service on target.
        /// </summary>
        protected TargetServiceFile Service
        {
            get { return _service; }
        }

        /// <summary>
        /// Gets the initial path, which is the root of enumeration.
        /// </summary>
        protected string Path
        {
            get { return _path; }
        }

        #endregion

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

            // gets initial info about path we are about to enumerate:
            TargetFile descriptor;

            try
            {
                descriptor = LoadInitialInfo(_path);
            }
            catch (Exception ex)
            {
                descriptor = null;
                QTraceLog.WriteException(ex, "Failed to initialize startup description");
                visitor.Failure(null, ex, "Failed to initialize startup description");
            }

            try
            {
                visitor.Begin(_service, descriptor);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed to initialize visitor");
                visitor.Failure(descriptor, ex, "Failed to initialize visitor");
            }

            try
            {
                if (descriptor != null)
                {
                    PerformEnumeration(visitor, descriptor);
                }
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed enumeration over \"{0}\"", descriptor != null ? descriptor.Path : "- unknown path -");
                visitor.Failure(descriptor, ex, "Failed to enumerate");
            }

            try
            {
                visitor.End();
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed to clean-up visitor");
                visitor.Failure(descriptor, ex, "Failed to clean-up visitor");
            }
        }

        /// <summary>
        /// Method to visit the whole file system deep-down, starting at given point and read all info around including file contents.
        /// Then it's the visitors responsibility to use received data and save it anyhow (keep in memory, store to local file system or ZIP).
        /// </summary>
        private void PerformEnumeration(IFileServiceVisitor visitor, TargetFile descriptor)
        {
            Stack<TargetFile> items = new Stack<TargetFile>();
            items.Push(descriptor);

            while (items.Count > 0 && !visitor.IsCancelled)
            {
                var item = items.Pop();

                try
                {
                    // do we have an access to open that item?
                    if (item.NoAccess)
                    {
                        visitor.UnknownEntering(item);
                        continue;
                    }

                    if (item.IsDirectory)
                    {
                        // list items inside the directory and add to queue to visit them:
                        visitor.DirectoryEntering(item);

                        var childItems = PerformDirectoryListing(item);
                        if (childItems != null)
                        {
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
                    }
                    else
                    {
                        // download a file and also treat all named-pipes, sockets etc as file and download any content if possible...
                        PerformFileRead(visitor, item);
                    }
                }
                catch (Exception ex)
                {
                    QTraceLog.WriteException(ex, "Failure during data transfer");
                    visitor.Failure(item, ex, "Failure during data transfer");
                }
            }
        }

        #endregion

        protected abstract TargetFile LoadInitialInfo(string path);
        protected abstract void PerformFileRead(IFileServiceVisitor visitor, TargetFile descriptor);
        protected abstract TargetFile[] PerformDirectoryListing(TargetFile descriptor);
    }
}
