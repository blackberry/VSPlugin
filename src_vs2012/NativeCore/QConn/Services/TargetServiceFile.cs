using System;
using System.Collections.Generic;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Class to communicate with a File-System Service on target.
    /// It allows any file and directory manipulations.
    /// </summary>
    public sealed class TargetServiceFile : TargetService
    {
        public const int ModeOpenNone = 0;
        public const int ModeOpenReadOnly= 1;
        public const int ModeOpenWriteOnly = 2;
        public const int ModeOpenReadWrite = 3;

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

            if (mode == ModeOpenNone)
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
                return new TargetFileDescriptor(this, null, permissions, 4096, 0, path, path);

            return new TargetFileDescriptor(this, handle, response[2].UInt32Value, response[3].UInt64Value, 0, response[4].StringValue, path);
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
            var reader = Connection.Request(string.Concat("r:", descriptor.Handle, ":", offset.ToString("X"), ":", length.ToString("X")));

            // read and parse the header part:
            var responseHeader = reader.ReadString(uint.MaxValue, '\n');
            if (string.IsNullOrEmpty(responseHeader))
                throw new QConnException("Unable to retrieve response header");
            var response = Token.Parse(responseHeader);
            if (response[0].StringValue != "o")
                throw new QConnException("Reading failed: " + response[1].StringValue);

            ulong contentLength = response[1].UInt64Value;
            // ulong contentOffset = response[2].UInt64Value;
            var buffer = reader.ReadBytes((int)contentLength);
            if (buffer == null || buffer.Length != (int)contentLength)
                throw new QConnException("Invalid number of content bytes read");

            return buffer;
        }

        /// <summary>
        /// Gets the info about specified path.
        /// If specified, it throws exceptions in case of any errors or permission denies.
        /// </summary>
        private TargetFileDescriptor Stat(string path, bool throwOnFailure)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            using (var descriptor = Open(path, ModeOpenNone, uint.MaxValue, throwOnFailure))
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
                using (var directory = Open(location.Path, ModeOpenReadOnly, TargetFile.TypeDirectory, true))
                {
                    // PH: HINT:
                    // if folder reports no size (like /tmp/slogger2), try to read as much as possible...
                    // just fingers crossed, it won't really try to allocated 2GB of memory...
                    var data = Read(directory, 0, directory.Size > 0 ? directory.Size : int.MaxValue);
                    if (data == null)
                        throw new QConnException("Invalid directory listing read");
                    string listing = Encoding.UTF8.GetString(data);

                    // parse names, as each is in separate line:
                    var result = new List<TargetFile>();
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
                                result.Add(statInfo);
                            }
                            else
                            {
                                QTraceLog.WriteLine("Unable to load info about path: \"" + itemPath + "\"");

                                // add a stub, just to keep the path only (as might have lack permissions to read info):
                                result.Add(new TargetFile(itemPath));
                            }
                        }
                    }

                    return result.ToArray();
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

            using (var descriptor = Open(path, 0x100, (permissions & TargetFile.TypeMask) | TargetFile.TypeDirectory, true))
            {
                return descriptor;
            }
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
    }
}
