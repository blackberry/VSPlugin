using System;
using System.Diagnostics;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.Package.Diagnostics;

namespace BlackBerry.Package.Components
{
    /// <summary>
    /// Manager class to record all logs of specified categories into persistent storage.
    /// It also limits the number of files, if required.
    /// </summary>
    internal static class LogManager
    {
        private static string _path;
        private static string[] _categories;
        private static PersistentTraceListener _recorder;

        /// <summary>
        /// Initializes recording logs.
        /// </summary>
        public static void Initialize(string path, int limitLogs, params string[] categories)
        {
            _path = path;
            _categories = categories;

            MountLogRecorder();
            Cleanup(limitLogs);
        }

        /// <summary>
        /// Switches to another folder or disables existing recorder.
        /// </summary>
        public static void Update(string path, int limitLogs)
        {
            if (string.CompareOrdinal(path, _path) != 0)
            {
                _path = path;

                // remove the log recorder from old location and setup in new location:
                UnmountLogRecorder();
                MountLogRecorder();
            }

            Cleanup(limitLogs);
        }

        /// <summary>
        /// Removes existing log recorder.
        /// </summary>
        private static void UnmountLogRecorder()
        {
            if (_recorder != null)
            {
                string name = _recorder.FileName;

                Trace.Listeners.Remove(_recorder);
                _recorder.Dispose();
                _recorder = null;

                TraceLog.WriteLine("Removed log recorder: \"{0}\"", name);
            }
        }

        /// <summary>
        /// Creates new log recorder.
        /// </summary>
        private static void MountLogRecorder()
        {
            if (_recorder == null && !string.IsNullOrEmpty(_path))
            {
                try
                {
                    _recorder = new PersistentTraceListener(_path, true, _categories);
                    Trace.Listeners.Add(_recorder);
                    TraceLog.WriteLine("Created log recorder: \"{0}\"", _recorder.FileName);
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to setup persistent logger");
                }
            }
        }

        /// <summary>
        /// Removes unnecessary log files, if their count is above the limit.
        /// </summary>
        private static void Cleanup(int limit)
        {
            if (limit > 0 && Directory.Exists(_path))
            {
                try
                {
                    var files = Directory.GetFiles(_path, PersistentTraceListener.Prefix + "*"); // get all file logs
                    if (files.Length > limit)
                    {
                        for (int i = 0; i < files.Length - limit; i++)
                        {
                            try
                            {
                                File.Delete(files[i]);
                            }
                            catch (Exception ex)
                            {
                                TraceLog.WriteException(ex, "Unable to delete file: \"{0}\"", files[i]);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to clean up log folder");
                }
            }
        }
    }
}
