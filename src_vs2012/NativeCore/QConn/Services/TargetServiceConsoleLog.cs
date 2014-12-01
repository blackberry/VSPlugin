using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Artificial service helping in capturing runtime console logs for processes.
    /// It simply continuously reads log files and passes its content via dedicated event.
    /// </summary>
    public sealed class TargetServiceConsoleLog : TargetService
    {
        #region Internal Classes

        /// <summary>
        /// Helper class storing current state of log file to read.
        /// </summary>
        sealed class LogMonitorStatus : IDisposable
        {
            private ulong _readBytes;
            private readonly List<byte[]> _chunks;

            /// <summary>
            /// Init constructor.
            /// </summary>
            public LogMonitorStatus(TargetServiceFile fileService, string path, TargetFileDescriptor handle, ProcessInfo process)
            {
                if (fileService == null)
                    throw new ArgumentNullException("fileService");
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException("path");
                if (handle == null)
                    throw new ArgumentNullException("handle");
                if (process == null)
                    throw new ArgumentNullException("process");

                FileService = fileService;
                Path = path;
                Handle = handle;
                Process = process;
                _chunks = new List<byte[]>();
            }

            ~LogMonitorStatus()
            {
                Dispose(false);
            }

            #region Properties

            public TargetServiceFile FileService
            {
                get;
                private set;
            }

            public string Path
            {
                get;
                private set;
            }

            public TargetFileDescriptor Handle
            {
                get;
                private set;
            }

            public ProcessInfo Process
            {
                get;
                private set;
            }

            #endregion

            /// <summary>
            /// Read all new log entries, if available or null.
            /// </summary>
            public string[] Read()
            {
                try
                {
                    const int ChunkSize = 20 * 1024;

                    // read one or more available chunks of data from the file, than glue them into one buffer,
                    // convert to UTF8 string and split per lines:
                    while (true)
                    {
                        var data = FileService.Read(Handle, _readBytes, ChunkSize);

                        if (data != null && data.Length > 0)
                        {
                            _readBytes += (ulong) data.Length;
                            if (data.Length == ChunkSize)
                            {
                                // store the chunk and try again:
                                _chunks.Add(data);
                                continue;
                            }

                            if (_chunks.Count > 0)
                            {
                                return GetString(BitHelper.Combine(_chunks, null, 0));
                            }

                            return GetString(data);
                        }

                        // if there are no more new chunks, but we did read any previously:
                        if (_chunks.Count > 0)
                        {
                            return GetString(BitHelper.Combine(_chunks, null, 0));
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to read from log file: \"{0}\"", Path);
                }

                return null;
            }

            private static bool IsWhiteSpace(byte c)
            {
                return c <= 32;
            }

            private static string[] GetString(byte[] data)
            {
                // since new-line will be automatically added at the end of log entry
                // trim-end now, before doing any conversions to save processor & memory:
                int length = data.Length;

                while (length > 0 && IsWhiteSpace(data[length - 1]))
                {
                    length--;
                }

                return Encoding.UTF8.GetString(data, 0, length).Split(new[] { "\r\n", "\n\r", "\n" }, StringSplitOptions.None);
            }

            public bool HasPath(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                return string.Compare(Path, path, StringComparison.Ordinal) == 0;
            }

            #region IDisposable Implementation

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Handle != null)
                    {
                        Handle.Close();
                    }
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion
        }

        #endregion

        private const uint Interval = 800; // how often to check the remote log file for new content; in milliseconds

        private TargetServiceFile _fileService;
        private readonly object _lock;
        private readonly List<LogMonitorStatus> _logMonitors;
        private Timer _keepReadingTimer;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceConsoleLog(TargetServiceFile fileService)
            : base(new Version(1, 0), fileService != null ? fileService.Connection : null)
        {
            if (fileService == null)
                throw new ArgumentNullException("fileService");

            _fileService = fileService;
            _lock = new object();
            _logMonitors = new List<LogMonitorStatus>();
        }

        protected override void Dispose(bool disposing)
        {
            StopAll();

            // first close the original service, that should close the connection, if opened:
            if (_fileService != null)
            {
                _fileService.Dispose();
                _fileService = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Starts monitoring of the logs printed on console for specified process.
        /// </summary>
        public bool Start(ProcessInfo process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            TraceLog.WriteLine("> Starting console-logs monitor for: {0}", process.Name);

            // PH: this is the same 'trick' used by Momentics, since console dumps everything into a log file
            //     let's continuously read that file and truncate it, if needed:

            // calculate path to the log file (the dummy way, without using $HOME$ variable):
            if (process.ExecutablePath != null)
            {
                var homeIndex = process.ExecutablePath.IndexOf("/app/", StringComparison.Ordinal);

                if (homeIndex > 0)
                {
                    var logFilePath = process.ExecutablePath.Substring(0, homeIndex) + "/logs/log";
                    return Start(logFilePath, process);
                }
            }

            TraceLog.WarnLine("> Error: Unable to calculate location of the console log file");
            return false;
        }

        private bool Start(string logFilePath, ProcessInfo process)
        {
            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentNullException("logFilePath");
            if (process == null)
                throw new ArgumentNullException("process");

            TraceLog.WriteLine("> Log file: \"{0}\"", logFilePath);

            if (Find(logFilePath) == null)
            {
                // open the file:
                var fileHandle = _fileService.Open(logFilePath, TargetFile.ModeOpenReadOnly, uint.MaxValue, false);
                if (fileHandle != null)
                {
                    lock (_lock)
                    {
                        var status = new LogMonitorStatus(_fileService, logFilePath, fileHandle, process);
                        _logMonitors.Add(status);

                        // start the timer:
                        if (_keepReadingTimer == null)
                            _keepReadingTimer = new Timer(OnReaderTick, this, Interval, Timeout.Infinite);
                    }

                    return true;
                }

                TraceLog.WriteLine("> Error: Unable to open log file: \"{0}\"", logFilePath);
                return false;
            }

            TraceLog.WarnLine("> Warning: Log file is already monitored: \"{0}\"", logFilePath);
            return false;
        }

        /// <summary>
        /// Stops monitoring for all console logs.
        /// </summary>
        public void StopAll()
        {
            TraceLog.WriteLine("> Stopping all console-logs monitors");

            if (_keepReadingTimer != null)
            {
                _keepReadingTimer.Dispose();
                _keepReadingTimer = null;
            }

            lock (_lock)
            {
                foreach (var monitor in _logMonitors)
                {
                    monitor.Dispose();
                }

                _logMonitors.Clear();
            }
        }

        /// <summary>
        /// Finds existing monitor that is attached to the specified log file or null.
        /// </summary>
        private LogMonitorStatus Find(string path)
        {
            lock (_lock)
            {
                foreach (var monitor in _logMonitors)
                {
                    if (monitor.HasPath(path))
                    {
                        return monitor;
                    }
                }
            }

            return null;
        }

        private void OnReaderTick(object state)
        {
            lock (_lock)
            {
                // ask each monitor, if there are now log entries and forward them onto the console:
                foreach (var monitor in _logMonitors)
                {
                    var logEntries = monitor.Read();
                    if (logEntries != null)
                    {
                        foreach (var message in logEntries)
                        {
                            TraceLog.WriteLine(message);
                            Trace.WriteLine(message, TraceLog.CategoryDevice);
                        }
                    }
                }
            }

            // ask the timer to run again, this will eliminate the problem
            // of low network speeds, when accessing the log file for all monitors took more than Interval:
            if (_keepReadingTimer != null)
            {
                _keepReadingTimer.Change(Interval, Timeout.Infinite);
            }
        }
    }
}
