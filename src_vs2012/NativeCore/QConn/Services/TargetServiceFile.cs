using System;
using System.Collections.Generic;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
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
        private const uint ModeOpenCreate = 0x100;
        private const uint ModeOpenTruncate = 0x200;

        internal const int DownloadUploadChunkSize = 48 * 1024;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceFile(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                // disconnect with target service:
                Post("q");
            }

            base.Dispose(disposing);
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
        /// Sends a command to the target and ignores the response.
        /// </summary>
        private void Post(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            // send:
            var rawResponse = Connection.Post(command);
        }

        /// <summary>
        /// Opens specified path with specified mode.
        /// </summary>
        internal TargetFileDescriptor Open(string path, uint mode, uint permissions, bool throwOnFailure)
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
                    throw new QConnException(string.Concat("Opening-handle failed: ", response[1].StringValue, " (", path, ")"));
                QTraceLog.WriteLine(string.Concat("Opening-handle failed: ", response[1].StringValue, " (", path, ")"));
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

            if (!descriptor.IsClosed && !IsDisposed)
            {
                var response = Send("c:" + descriptor.Handle);
                descriptor.Closed();

                if (response[0].StringValue == "e")
                    throw new QConnException("Closing-handle failed: " + response[1].StringValue);
            }
        }

        /// <summary>
        /// Reads binary data directly from a file or folder of the target at specified offset.
        /// </summary>
        internal byte[] Read(TargetFileDescriptor descriptor, ulong offset, ulong length)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (descriptor.IsClosed)
                throw new ArgumentOutOfRangeException("descriptor");
            if (!descriptor.CanRead)
                throw new QConnException("File is not opened for read");
            if (length == 0)
                throw new ArgumentOutOfRangeException("length", "Too few data to read requested");
            if (length > int.MaxValue)
                throw new ArgumentOutOfRangeException("length", "Unable to load so much data at once");

            // ask for the raw data:
            var command = string.Concat("r:", descriptor.Handle, ":", offset.ToString("X"), ":", length.ToString("X"));
            var reader = Connection.Request(command);
            if (reader == null)
                throw new QConnException("Invalid response to read request received");

            // read and parse the header part:
            var responseHeader = reader.ReadString(uint.MaxValue, '\r');
            reader.Skip(1); // skip '\n'
            if (string.IsNullOrEmpty(responseHeader))
                throw new QConnException("Unable to retrieve response header");
            var response = Token.Parse(responseHeader);
            if (response[0].StringValue != "o" && response[0].StringValue != "o+")
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
        /// Writes specified amount of data into an open file.
        /// </summary>
        internal uint Write(TargetFileDescriptor descriptor, byte[] data)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (data == null)
                throw new ArgumentNullException("data");

            return Write(descriptor, data, 0, data.Length);
        }

        /// <summary>
        /// Writes specified amount of data into an open file.
        /// </summary>
        internal uint Write(TargetFileDescriptor descriptor, byte[] data, int offset, int length)
        {
            if (descriptor == null)
                throw new ArgumentNullException("descriptor");
            if (descriptor.IsClosed)
                throw new ArgumentOutOfRangeException("descriptor");
            if (!descriptor.CanWrite)
                throw new QConnException("File is not opened for write");
            if (data == null)
                throw new ArgumentNullException("data");
            if (offset > data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (data.Length < offset + length)
                throw new ArgumentOutOfRangeException("length", "Specified data buffer is too short");

            // send file content:
            var command = string.Concat("w:", descriptor.Handle, ":0:", length.ToString("X"), ":1"); // last 1 means appending
            var responseContent = Connection.Send(command, data, offset, length, true);

            var response = Token.Parse(responseContent);
            if (response[0].StringValue != "o")
                throw new QConnException("Reading failed: " + response[1].StringValue);

            uint writeLength = response[1].UInt32Value;
            // uint contentOffset = response[2].UInt64Value;

            return writeLength;
        }

        /// <summary>
        /// Gets the info about specified path.
        /// If specified, it throws exceptions in case of any errors or permission denies.
        /// </summary>
        internal TargetFile Stat(string path, bool throwOnFailure)
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
                    if (response.Length > 5)
                    {
                        var creationTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(response[9].UInt64Value).ToLocalTime();
                        descriptor.Update(response[5].UInt32Value, response[6].UInt32Value, creationTime, response[10].UInt32Value, response[2].UInt64Value);
                    }
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
                                //QTraceLog.WriteLine("Unable to load info about path: \"" + itemPath + "\"");           // PH: for debugging only; this log only duplicates error printouts from Stat() call

                                // add a stub, just to keep the path only (as might have lack permissions to read info):
                                statInfo = new TargetFile(itemPath, item, false);
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
        public TargetFile CreateFolder(string fullPath, uint permissions)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentNullException("fullPath");

            // does it exist?
            using (var descriptor = Open(fullPath, TargetFile.ModeOpenNone, (permissions & TargetFile.TypeMask) | TargetFile.TypeDirectory, false))
            {
                if (descriptor != null)
                    return descriptor;
            }

            // try to create directory:
            using (var descriptor = Open(fullPath, ModeOpenCreate, (permissions & TargetFile.TypeMask) | TargetFile.TypeDirectory, false))
            {
                if (descriptor != null)
                    return descriptor;
            }

            // if creation failed, maybe some parent folders on the path are missing, try to create them:
            var parent = CreateFolder(PathHelper.ExtractDirectory(fullPath), permissions);
            if (parent == null)
                throw new QConnException("Failed to create folder");

            // try to create directory again throwing exception if failed:
            using (var descriptor = Open(fullPath, ModeOpenCreate, (permissions & TargetFile.TypeMask) | TargetFile.TypeDirectory, true))
            {
                return descriptor;
            }
        }

        /// <summary>
        /// Creates a folder at specified location.
        /// </summary>
        public TargetFile CreateFolder(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentNullException("fullPath");

            return CreateFolder(fullPath, TargetFile.ModePermissionsAll);
        }

        /// <summary>
        /// Creates new sub-folder at specified location.
        /// </summary>
        public TargetFile CreateFolder(TargetFile location, string name)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (location.NoAccess)
                throw new ArgumentOutOfRangeException("location");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return CreateFolder(PathHelper.MakePath(location.Path, name), TargetFile.ModePermissionsAll);
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
        /// Removes the whole directory tree.
        /// </summary>
        public void RemoveTree(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Remove(path, 1u);
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
        /// Downloads files and folders (including whole subtree) from specified location on target and passes them to visitor for further processing.
        /// </summary>
        public IFileServiceVisitorMonitor DownloadAsync(string targetPath, IFileServiceVisitor visitor)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");
            if (visitor == null)
                throw new ArgumentNullException("visitor");

            return EnumerateAsync(new TargetEnumerator(targetPath), visitor) as IFileServiceVisitorMonitor;
        }

        /// <summary>
        /// Downloads files and folders (including whole subtree) from specified location on target and saves them locally.
        /// </summary>
        public IFileServiceVisitorMonitor DownloadAsync(string targetPath, string localPath, object tag)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");
            if (string.IsNullOrEmpty(localPath))
                throw new ArgumentNullException("localPath");

            return EnumerateAsync(new TargetEnumerator(targetPath), new LocalCopyVisitor(localPath, tag)) as IFileServiceVisitorMonitor;
        }

        /// <summary>
        /// Gets a specified collection of files and folders and uploads them to target at specified location.
        /// </summary>
        public TargetCopyVisitor UploadAsync(IFileServiceEnumerator enumerator, string targetPath, object tag)
        {
            if (enumerator == null)
                throw new ArgumentNullException("enumerator");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            return (TargetCopyVisitor) EnumerateAsync(enumerator, new TargetCopyVisitor(targetPath, tag));
        }

        /// <summary>
        /// Gets collection of files and folders (including whole subtree) from specified location on current desktop machine and uploads them to target at specified location.
        /// </summary>
        public TargetCopyVisitor UploadAsync(string localPath, string targetPath, object tag)
        {
            if (string.IsNullOrEmpty(localPath))
                throw new ArgumentNullException("localPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            return (TargetCopyVisitor) EnumerateAsync(new LocalEnumerator(localPath), new TargetCopyVisitor(targetPath, tag));
        }

        /// <summary>
        /// Loads specified file or folders (including whole subtree) into memory.
        /// </summary>
        public BufferVisitor PreviewAsync(string targetPath)
        {
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            return (BufferVisitor) EnumerateAsync(new TargetEnumerator(targetPath), new BufferVisitor());
        }

        /// <summary>
        /// Enumerates asynchronously the content of the file or folder (including whole subtree) and passes them to specified visitor.
        /// It allows then more advanced processing like:
        ///  * loading specified files into memory
        ///  * storing them in local file-system
        ///  * zipping them and storing locally
        /// and also:
        ///  * waiting for completion
        ///  * progress monitoring
        ///
        /// Check the implemented visitors for more details.
        /// </summary>
        public IFileServiceVisitor EnumerateAsync(IFileServiceEnumerator enumerator, IFileServiceVisitor visitor)
        {
            if (enumerator == null)
                throw new ArgumentNullException("enumerator");
            if (visitor == null)
                throw new ArgumentNullException("visitor");

            // clone current QConn-connection to allow asynchronous processing (otherwise it could interfere with other sync commands)
            // and arm clone with a bigger buffer, to faster load data:
            var connection = (QConnConnection) Connection.Clone(DownloadUploadChunkSize);
            if (connection == null)
                throw new QConnException("Unable to establish asynchronous connection");

            // connect:
            connection.Open();

            // load info about the file to download:
            var service = new TargetServiceFile(Version, connection);
            enumerator.Begin(service);

            // HINT: service will be automatically disposed in completion callback below:
            var asyncHandler = new Action<IFileServiceVisitor>(enumerator.Enumerate);
            asyncHandler.BeginInvoke(visitor, DownloadAsyncCompleted, new Tuple<IFileServiceEnumerator, TargetServiceFile, object>(enumerator, service, asyncHandler));

            return visitor;
        }

        private void DownloadAsyncCompleted(IAsyncResult ar)
        {
            var data = (Tuple<IFileServiceEnumerator, TargetServiceFile, object>)ar.AsyncState;
            var enumerator = data.Item1;
            var service = data.Item2;
            var action = (Action<IFileServiceVisitor>)data.Item3;

            try
            {
                action.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Asynchronous download failed");
            }

            try
            {
                enumerator.End();
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Enumerator cleaning-up failed");
            }

            try
            {
                service.Dispose();
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Service cleaning-up failed");
            }
        }
    }
}
