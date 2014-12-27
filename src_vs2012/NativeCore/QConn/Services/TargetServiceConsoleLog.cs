using System;
using System.Collections.Generic;
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

            public event EventHandler<EventArgs> Finished;

            /// <summary>
            /// Init constructor.
            /// </summary>
            public LogMonitorStatus(TargetServiceFile fileService, string path, TargetFileDescriptor handle, ProcessInfo process, bool isDebugging)
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
                IsDebugging = isDebugging;
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

            /// <summary>
            /// Gets an indication, if this log monitoring is attached to debugged app or not.
            /// </summary>
            public bool IsDebugging
            {
                get;
                private set;
            }

            #endregion

            /// <summary>
            /// Resets the reading offset to start logs from the beginning.
            /// </summary>
            public void Reset()
            {
                _readBytes = 0;
            }

            /// <summary>
            /// Read all new log entries, if available or null.
            /// </summary>
            public string[] Read()
            {
                try
                {
                    const int ChunkSize = 20 * 1024;

                    _chunks.Clear();

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

                    if (Finished != null)
                        Finished(this, EventArgs.Empty);
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

                    Finished = null;
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

        public const uint DefaultInterval = 750; // how often to check the remote log file for new content; in milliseconds

        private TargetServiceFile _fileService;
        private readonly object _sync;
        private readonly List<LogMonitorStatus> _logMonitors;
        private uint[] _processIDs;
        private uint _interval;
        private Timer _keepReadingTimer;

        /// <summary>
        /// Event notifying that new console log entries are available for processing.
        /// </summary>
        public event EventHandler<CapturedLogsEventArgs> Captured;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceConsoleLog(TargetServiceFile fileService)
            : base(new Version(1, 0), fileService != null ? fileService.Connection : null)
        {
            if (fileService == null)
                throw new ArgumentNullException("fileService");

            _fileService = fileService;
            _sync = new object();
            _logMonitors = new List<LogMonitorStatus>();
            _interval = DefaultInterval;
            UpdateProcessIDsNoSync();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                StopAll();
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to stop console-logs for all monitored processes");
            }

            // first close the original service, that should close the connection, if opened:
            if (_fileService != null)
            {
                _fileService.Dispose();
                _fileService = null;
            }

            Captured = null;
            base.Dispose(disposing);
        }

        #region Properties

        /// <summary>
        /// Gets an array of PIDs of monitored logs.
        /// </summary>
        public uint[] ProcessIDs
        {
            get { return _processIDs; }
        }

        /// <summary>
        /// Gets an indication, if there is anything monitored at the moment.
        /// </summary>
        public bool IsMonitoringAnything
        {
            get { return _processIDs != null && _processIDs.Length > 0; }
        }

        /// <summary>
        /// Gets or sets the interval of pinging the logs for new content.
        /// </summary>
        public uint Interval
        {
            get { return _interval; }
            set
            {
                _interval = value == 0 ? DefaultInterval : value; // it will automatically pickup this value on next round of log refresh
            }
        }

        #endregion

        /// <summary>
        /// Starts monitoring of the logs printed on console for specified process.
        /// </summary>
        public bool Start(ProcessInfo process, uint interval, bool isDebugging)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            TraceLog.WriteLine("> Starting console-logs monitor for: {0}", process.Name);
            Interval = interval;

            // PH: this is the same 'trick' used by Momentics, since console dumps everything into a log file
            //     let's continuously read that file and truncate it, if needed:

            // calculate path to the log file (the dummy way, without using $HOME$ variable):
            if (process.ExecutablePath != null)
            {
                var homeIndex = process.ExecutablePath.IndexOf("/app/", StringComparison.Ordinal);

                if (homeIndex > 0)
                {
                    var logFilePath = process.ExecutablePath.Substring(0, homeIndex) + "/logs/log";
                    return Start(logFilePath, process, interval, isDebugging);
                }
            }

            TraceLog.WarnLine("> Error: Unable to calculate location of the console log file");
            return false;
        }

        private bool Start(string logFilePath, ProcessInfo process, uint interval, bool isDebugging)
        {
            if (string.IsNullOrEmpty(logFilePath))
                throw new ArgumentNullException("logFilePath");
            if (process == null)
                throw new ArgumentNullException("process");

            TraceLog.WriteLine("> Log file: \"{0}\"", logFilePath);

            var existingMonitor = Find(logFilePath);

            if (existingMonitor == null)
            {
                // open the file:
                var fileHandle = _fileService.Open(logFilePath, TargetFile.ModeOpenReadOnly, uint.MaxValue, false);
                if (fileHandle != null)
                {
                    lock (_sync)
                    {
                        var monitor = new LogMonitorStatus(_fileService, logFilePath, fileHandle, process, isDebugging);
                        monitor.Finished += OnMonitorStatusFinished;
                        _logMonitors.Add(monitor);
                        UpdateProcessIDsNoSync();

                        // start the timer:
                        if (_keepReadingTimer == null)
                            _keepReadingTimer = new Timer(OnReaderTick, this, interval, Timeout.Infinite);
                    }

                    return true;
                }

                TraceLog.WriteLine("> Error: Unable to open log file: \"{0}\"", logFilePath);
                return false;
            }

            existingMonitor.Reset();
            TraceLog.WarnLine("> Warning: Log file is already monitored, resetting: \"{0}\"", logFilePath);
            return false;
        }

        private void OnMonitorStatusFinished(object sender, EventArgs e)
        {
            var status = (LogMonitorStatus) sender;
            status.Finished -= OnMonitorStatusFinished;

            try
            {
                Stop(status.Process);
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Failed to stop console-logs monitor for: {0}", status.Process != null ? status.Process.Name : "unknown process");
            }
        }

        /// <summary>
        /// Checks, if console outputs are specified for specified process.
        /// </summary>
        public bool IsMonitoring(ProcessInfo process)
        {
            if (process == null)
                return false;

            lock (_sync)
            {
                return FindNoSync(process) != null;
            }
        }

        /// <summary>
        /// Stops monitoring of console outputs for specified process.
        /// </summary>
        public bool Stop(ProcessInfo process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            bool killTimer = false;
            LogMonitorStatus monitor;

            lock (_sync)
            {
                monitor = FindNoSync(process);

                if (monitor != null)
                {
                    _logMonitors.Remove(monitor);
                    killTimer = _logMonitors.Count == 0;
                    UpdateProcessIDsNoSync();
                }
            }

            if (killTimer)
            {
                if (_keepReadingTimer != null)
                {
                    _keepReadingTimer.Dispose();
                    _keepReadingTimer = null;
                }
            }

            if (monitor != null)
            {
                TraceLog.WriteLine("> Stopping console-logs monitor for: \"{0}\"", monitor.Path);
                monitor.Dispose();

                return true;
            }

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

            lock (_sync)
            {
                foreach (var monitor in _logMonitors)
                {
                    monitor.Dispose();
                }

                _logMonitors.Clear();
                UpdateProcessIDsNoSync();
            }
        }

        /// <summary>
        /// Checks, if log file for specified process is monitored.
        /// </summary>
        public bool IsMonitoring(uint processID)
        {
            var processes = _processIDs;
            foreach (uint procID in processes)
            {
                if (procID == processID)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Finds existing monitor that is attached to the specified log.
        /// In case of non-existence it returns null.
        /// </summary>
        private LogMonitorStatus Find(string path)
        {
            lock (_sync)
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

        /// <summary>
        /// Finds existing monitor that is attached to the specified process.
        /// In case of non-existence it returns null.
        /// </summary>
        private LogMonitorStatus FindNoSync(ProcessInfo process)
        {
            if (process == null)
                return null;

            foreach (var monitor in _logMonitors)
            {
                if (monitor.Process.ID == process.ID || string.Compare(monitor.Process.ExecutablePath, process.ExecutablePath, StringComparison.Ordinal) == 0)
                {
                    return monitor;
                }
            }

            return null;
        }

        private void UpdateProcessIDsNoSync()
        {
            // copy the PIDs into separate array:
            var newArray = new uint[_logMonitors.Count];
            for (int i = 0; i < _logMonitors.Count; i++)
            {
                newArray[i] = _logMonitors[i].Process.ID;
            }

            // PH: and replace the whole array,
            //     since this array changes rarely, thanks to this trick, none of the external code,
            //     which only enumerates over this array need any sync-locks!
            _processIDs = newArray;
        }

        private void OnReaderTick(object state)
        {
            lock (_sync)
            {
                // ask each monitor, if there are now log entries and forward them onto the console:
                for(int i = 0; i < _logMonitors.Count; i++)
                {
                    var monitor = _logMonitors[i];
                    var logEntries = monitor.Read();
                    if (logEntries != null && Captured != null)
                    {
                        var targetLogs = TargetLogEntry.ParseConsole(monitor.Process, logEntries);

                        if (targetLogs != null)
                        {
                            Captured(this, new CapturedLogsEventArgs(targetLogs));
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
