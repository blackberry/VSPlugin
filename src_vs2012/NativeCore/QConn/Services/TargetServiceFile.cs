using System;
using System.Collections.Generic;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Visitors;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Class to communicate with a File-System Service on target.
    /// It allows any file and directory manipulations.
    /// </summary>
    public sealed class TargetServiceFile : TargetService
    {
        private const int ModeOpenAppend = 8;
        private const int ModeOpenCreate = 0x100;
        private const int ModeOpenTruncate = 0x200;
        private const int ModeOpenExclude = 0x400;

        private const int DownloadUploadChunkSize = 8192;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceFile(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        public override string ToString()
        {
            return "FileService";
        }

        /// <summary>
        /// Sends a command to the target and returns its parsed representation.
        /// </summary>
        private Token[] Send(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            // send:
            var rawResponse = Connection.Send(command);
            if (string.IsNullOrEmpty(rawResponse))
                throw new QConnException("Invalid response received for command: \"" + command + "\"");

            // parse:
            var response = Token.Parse(rawResponse);
            if (response == null || response.Length == 0)
                throw new QConnException("Unable to parse response: \"" + rawResponse + "\" for command: \"" + command + "\"");

            return response;
        }

        /// <summary>
        /// Opens specified path with specified mode.
        /// </summary>
        private TargetFileDescriptor Open(string path, uint mode, uint permissions, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Token[] response;

            if (mode == TargetFile.ModeOpenNone)
            {
                response = Send(string.Concat("oc:\"", path, "\":0"));
            }
            else
            {
                uint qmode = (mode & 0xFFFFFFFC) | ((mode - 1) & 3);

                if (permissions != uint.MaxValue)
                {
                    response = Send(string.Concat("o:\"", path, "\":", qmode.ToString("X"), ":", permissions.ToString("X")));
                }
                else
                {
                    response = Send(string.Concat("o:\"", path, "\":", qmode.ToString("X")));
                }
            }

            // verify response to open request:
            if (response[0].StringValue == "e")
            {
                if (throwOnFailure)
                    throw new QConnException("Opening-handle failed: " + response[1].StringValue);
                QTraceLog.WriteLine("Opening-handle failed: " + response[1].StringValue);
                return null;
            }
            if (response.Length < 5)
            {
                if (throwOnFailure)
                    throw new QConnException("Opening-handle response has invalid format");

                QTraceLog.WriteLine("Opening-handle response has invalid format");
                return null;
            }

            var handle = response[1].StringValue;

            // creating folder, returns some dummy data...
            if (string.Compare(handle, "-1", StringComparison.Ordinal) == 0)
                return new TargetFileDescriptor(this, null, permissions, 4096, mode, path);

            return new TargetFileDescriptor(this, handle, response[2].UInt32Value, response[3].UInt64Value, mode, response[4].StringValue);
        }

        internal void Close(TargetFileDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");

            if (!descriptor.IsClosed)
            {
                var response = Send("c:" + descriptor.Handle);
                descriptor.Closed();

                if (response[0].StringValue == "e")
                    throw new QConnException("Closing-handle failed: " + response[1].StringValue);
            }
        }

        internal byte[] Read(TargetFileDescriptor descriptor, ulong offset, ulong length)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (descriptor.IsClosed)
                throw new ArgumentOutOfRangeException("descriptor");
            if (length == 0)
                throw new ArgumentOutOfRangeException("length", "Too few data to read requested");
            if (length > int.MaxValue)
                throw new ArgumentOutOfRangeException("length", "Unable to load so much data at once");

            // ask for the raw data:
            var command = string.Concat("r:", descriptor.Handle, ":", offset.ToString("X"), ":", length.ToString("X"));
            var reader = Connection.Request(command);

            // read and parse the header part:
            var responseHeader = reader.ReadString(uint.MaxValue, '\r');
            reader.Skip(1); // skip '\n'
            if (string.IsNullOrEmpty(responseHeader))
                throw new QConnException("Unable to retrieve response header");
            var response = Token.Parse(responseHeader);
            if (response[0].StringValue != "o")
                throw new QConnException("Reading failed: " + response[1].StringValue);

            ulong contentLength = response[1].UInt64Value;
            // ulong contentOffset = response[2].UInt64Value;

            if (contentLength > 0)
            {
                var buffer = reader.ReadBytes((int) contentLength);
                if (buffer == null || buffer.Length != (int) contentLength)
                    throw new QConnException("Invalid number of content bytes read");
                return buffer;
            }

            return new byte[0];
        }

        /// <summary>
        /// Gets the info about specified path.
        /// If specified, it throws exceptions in case of any errors or permission denies.
        /// </summary>
        private TargetFile Stat(string path, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            using (var descriptor = Open(path, TargetFile.ModeOpenNone, uint.MaxValue, throwOnFailure))
            {
                // when opening successful:
                if (descriptor != null)
                {
                    // ask for full description:
                    var response = Send("s:" + descriptor.Handle);
                    if (response[0].StringValue == "e")
                    {
                        if (throwOnFailure)
                            throw new QConnException("Stat failed: " + response[1].StringValue);

                        QTraceLog.WriteLine("Stat failed: " + response[1].StringValue);
                        return null;
                    }

                    // update creation-date, type and size:
                    var creationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(response[9].UInt64Value).ToLocalTime();
                    descriptor.Update(response[5].UInt32Value, response[6].UInt32Value, creationTime, response[10].UInt32Value, response[2].UInt64Value);
                }
                return descriptor;
            }
        }

        /// <summary>
        /// Gets the info about specified path.
        /// It throws exceptions in case of any errors or permission denies.
        /// </summary>
        public TargetFile Stat(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            return Stat(path, true);
        }

        /// <summary>
        /// Lists files and folders at specified location.
        /// </summary>
        public TargetFile[] List(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var descriptor = Stat(path, true);
            if (descriptor == null)
                throw new QConnException("Unable to determine path properties");
            return List(descriptor);
        }

        /// <summary>
        /// Lists files and folders at specified location.
        /// </summary>
        public TargetFile[] List(TargetFile location)
        {
            if (location == null)
                throw new ArgumentNullException("location");

            if (location.IsDirectory)
            {
                using (var directory = Open(location.Path, TargetFile.ModeOpenReadOnly, TargetFile.TypeDirectory, true))
                {
                    // PH: HINT:
                    // since some folders reports no size (like /tmp/slogger2) or too small size (like /pps/system only 8 bytes), try to read as much as possible...
                    // just fingers crossed, it won't really try to allocated 2GB of memory...
                    var data = Read(directory, 0, int.MaxValue);
                    if (data == null)
                        throw new QConnException("Invalid directory listing read");
                    string listing = Encoding.UTF8.GetString(data);

                    // parse names, as each is in separate line:
                    var result = new SortedList<TargetFile, TargetFile>();
                    var foundItems = listing.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var item in foundItems)
                    {
                        if (item != "." && item != "..")
                        {
                            string itemPath = location.CreateItemPath(item);

                            // and try to load detailed info:
                            var statInfo = Stat(itemPath, false);
                            if (statInfo != null)
                            {
                                statInfo.Update(item);
                            }
                            else
                            {
                                QTraceLog.WriteLine("Unable to load info about path: \"" + itemPath + "\"");

                                // add a stub, just to keep the path only (as might have lack permissions to read info):
                                statInfo = new TargetFile(itemPath, item);
                            }

                            result.Add(statInfo, statInfo);
                        }
                    }

                    // and return an non-mutable array:
                    var array = new TargetFile[result.Count];
                    result.Values.CopyTo(array, 0);
                    return array;
                }
            }

            throw new QConnException("Not a folder, unable to perform listing");
        }

        /// <summary>
        /// Create a folder at specified location.
        /// </summary>
        public TargetFile CreateFolder(string path, uint permissions)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            using (var descriptor = Open(path, ModeOpenCreate, (permissions & TargetFile.TypeMask) | TargetFile.TypeDirectory, true))
            {
                return descriptor;
            }
        }

        /// <summary>
        /// Creates a folder at specified location.
        /// </summary>
        public TargetFile CreateFolder(string path)
        {
            return CreateFolder(path, 0xFFF);
        }

        /// <summary>
        /// Creates an empty file on target.
        /// </summary>
        public TargetFile CreateFile(string path, uint permissions)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            using (var descriptor = CreateNewFile(path, permissions))
            {
                return descriptor;
            }
        }

        /// <summary>
        /// Opens a file for reading or writing on target.
        /// </summary>
        internal TargetFileDescriptor CreateNewFile(string path, uint permissions)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            return Open(path, ModeOpenCreate | ModeOpenTruncate | TargetFile.ModeOpenReadWrite, permissions & TargetFile.TypeMask, true);
        }

        /// <summary>
        /// Removes the file or folder at specified location.
        /// </summary>
        public void Remove(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Remove(path, 0u);
        }

        /// <summary>
        /// Removes the file or folder at specified location.
        /// </summary>
        public void Remove(string path, uint flags)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // ask to delete:
            var response = Send("d:\"" + path + "\":" + flags.ToString("X"));
            if (response[0].StringValue == "e")
                throw new QConnException("Remove failed: " + response[1].StringValue);
        }

        /// <summary>
        /// Moves the file or folder from specified source location to the destination location.
        /// </summary>
        public void Move(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                throw new ArgumentNullException("sourcePath");
            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentNullException("destinationPath");

            // ask to move:
            var response = Send("m:\"" + sourcePath + "\":\"" + destinationPath + "\"");
            if (response[0].StringValue == "e")
                throw new QConnException("Move failed: " + response[1].StringValue);
        }

        /// <summary>
        /// Downloads the content of the file or folder (including whole subtree) and passes them to specified visitor.
        /// It allows then more advanced processing like:
        ///  * loading files into memory
        ///  * storing them in local file-system
        ///  * zipping them and storing locally
        /// and also:
        ///  * waiting for completion
        ///  * progress monitoring
        ///
        /// Check the implemented visitors for more details.
        /// </summary>
        public IFileServiceVisitor DownloadAsync(string path, IFileServiceVisitor visitor)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (visitor == null)
                throw new ArgumentNullException("visitor");

            // clone current QConn-connection with a bigger buffer, to faster load data:
            var connection = (QConnConnection) Connection.Clone(DownloadUploadChunkSize);
            if (connection == null)
                throw new QConnException("Unable to establish asynchronous connection");

            // connect:
            connection.Open();

            // load info about the file to download:
            var service = new TargetServiceFile(Version, connection);
            var descriptor = service.Stat(path, false);
            if (descriptor == null)
            {
                descriptor = new TargetFile(path, null);
            }

            // HINT: service will be automatically disposed in completion callback:
            var asyncHandler = new Action<TargetServiceFile, TargetFile, IFileServiceVisitor>(DownloadAsyncWorker);
            asyncHandler.BeginInvoke(service, descriptor, visitor, DownloadAsyncCompleted, new KeyValuePair<TargetServiceFile, object>(service, asyncHandler));

            return visitor;
        }

        private void DownloadAsyncCompleted(IAsyncResult ar)
        {
            var data = (KeyValuePair<TargetServiceFile, object>) ar.AsyncState;
            var service = data.Key;
            var action = (Action<TargetServiceFile, TargetFile, IFileServiceVisitor>)data.Value;

            try
            {
                action.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Asynchronous download failed");
            }

            service.Dispose();
        }

        /// <summary>
        /// Method to visit the whole file system deep-down, starting at given point and read all info around including file contents.
        /// Then it's the visitors responsibility to use received data and save it anyhow (keep in memory, store to local file system or ZIP).
        /// </summary>
        private static void DownloadAsyncWorker(TargetServiceFile service, TargetFile descriptor, IFileServiceVisitor visitor)
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
                        DownloadAsyncReadFile(service, visitor, item);
                    }
                    else
                    {
                        if (item.IsDirectory)
                        {
                            // list items inside the directory and add to queue to visit them:
                            visitor.DirectoryEntering(item);
                            var childItems = service.List(item);

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
                            DownloadAsyncReadFile(service, visitor, item);
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

        private static void DownloadAsyncReadFile(TargetServiceFile service, IFileServiceVisitor visitor, TargetFile descriptor)
        {
            try
            {
                using (var file = service.Open(descriptor.Path, TargetFile.ModeOpenReadOnly, 0, false))
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
                                var data = service.Read(file, totalRead, DownloadUploadChunkSize);
                                if (data != null && data.Length > 0)
                                {
                                    totalRead += (ulong) data.Length;
                                    visitor.FileContent(descriptor, data, totalRead);

                                    // is last chunk?
                                    if (data.Length != DownloadUploadChunkSize)
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
