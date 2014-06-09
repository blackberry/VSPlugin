using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Text;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Runner, that calls specific tool to update specified target within the NDK (new API Level, new simulator or runtime).
    /// </summary>
    internal sealed class ApiLevelUpdateRunner : ToolRunner
    {
        #region Internal Classes

        /// <summary>
        /// Description of the file fetched from the server.
        /// </summary>
        [DebuggerDisplay("{Name} ({Read} / {Size})")]
        sealed class LogFetchFile
        {
            public LogFetchFile(string name)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentNullException("name");

                Name = name;
            }

            #region Properties

            public string Name
            {
                get;
                private set;
            }

            public long Read
            {
                get;
                set;
            }

            public long Size
            {
                get;
                set;
            }

            #endregion
        }

        public enum LogState
        {
            Waiting,
            Preparing,
            Downloading,
            Installing,
            Configuring,
            Completed
        }

        public sealed class LogParser
        {
            public event EventHandler<ApiLevelUpdateLogEventArgs> Log;

            private LogState _state;
            private readonly ApiLevelUpdateRunner _parent;
            private readonly List<LogFetchFile> _files;

            public LogParser(ApiLevelUpdateRunner parent)
            {
                if (parent == null)
                    throw new ArgumentNullException("parent");

                _state = LogState.Waiting;
                _parent = parent;
                _files = new List<LogFetchFile>();
            }

            public void Reset()
            {
                State = LogState.Waiting;
                _files.Clear();
                _parent.CanAbort = true;
            }

            /// <summary>
            /// Converts the log message from the runner into a notification with similar data.
            /// </summary>
            public void Consume(string line)
            {
                if (string.IsNullOrEmpty(line))
                    return;

                if (line.StartsWith("downloading ", StringComparison.OrdinalIgnoreCase))
                {
                    State = LogState.Downloading;

                    // ok, new file to download:
                    var name = line.Substring(12).Trim();
                    _files.Add(new LogFetchFile(name));

                    Notify(string.Concat("Preparing ", name, "..."), 10, true);
                    return;
                }

                if (line.StartsWith("fetching ", StringComparison.OrdinalIgnoreCase))
                {
                    long progress;
                    long size;
                    string originalFileName = ExtractFileName(line, out progress, out size);
                    var fileName = originalFileName;

                    var file = Get(fileName, _files);
                    if (file == null)
                    {
                        fileName = Path.GetFileNameWithoutExtension(fileName);
                        file = Get(fileName, _files);
                    }

                    if (file != null && size > 0)
                    {
                        file.Read = progress;
                        file.Size = size;
                    }

                    // show nice log message, this is the one developer sees most often:
                    var overallProgress = 10 + (int)((CalculateDownloadProgress() * 70d)); // expected from 10-80%
                    if (file != null)
                    {
                        const double MegaByte = 1024d * 1024d;
                        double dProgress = file.Read / MegaByte;
                        double dSize = (file.Size / MegaByte);
                        if (dSize < 1) // just a guard, to make it look like, there is always something to download
                            dSize = 1;

                        Notify(string.Concat("Downloading ", originalFileName, " (", dProgress.ToString("N2"), "MB of ", dSize.ToString("N2"), "MB)..."), overallProgress, true);
                    }
                    else
                    {
                        Notify(string.Concat("Downloading ", originalFileName, "..."), overallProgress, true);
                    }
                    return;
                }

                if (line.StartsWith("installing ", StringComparison.OrdinalIgnoreCase))
                {
                    var file = Find(line, _files);
                    if (file != null)
                    {
                        // mark as completed download
                        file.Read = file.Size;
                        State = LogState.Installing;

                        Notify(line, 80, false);
                        return;
                    }

                    Notify(line, 5, true);
                    return;
                }

                if (line.StartsWith("configuring ", StringComparison.OrdinalIgnoreCase))
                {
                    State = LogState.Configuring;
                    Notify(line, 90, false);
                    return;
                }

                if (line.StartsWith("preparing to commit ", StringComparison.OrdinalIgnoreCase))
                {
                    Notify(line, 95, false);
                    return;
                }

                if (line.StartsWith("committing ", StringComparison.OrdinalIgnoreCase))
                {
                    State = LogState.Completed;
                    Notify(line, 100, false);
                    return;
                }
            }

            private double CalculateDownloadProgress()
            {
                // go through all files and measure global progress:
                long size = 0;
                long read = 0;

                foreach (var file in _files)
                {
                    read += file.Read;
                    size += file.Size;
                }

                return size == 0 ? 0d : read / (double) size;
            }

            private string ExtractFileName(string text, out long progress, out long size)
            {
                progress = 0;
                size = 0;

                if (string.IsNullOrEmpty(text))
                    return null;

                int startAt = text.IndexOf(' ');    // from first space
                int endAt = text.IndexOf('(');      // till the bracket with progress

                if (startAt < 0 || endAt < 0)
                    return null;

                var name = text.Substring(startAt + 1, endAt - startAt - 1).Trim();

                int progressEndAt = text.IndexOf(" of ", endAt, StringComparison.OrdinalIgnoreCase);
                int sizeEndAt = text.IndexOf(" at ", endAt, StringComparison.OrdinalIgnoreCase);

                if (progressEndAt < 0 || sizeEndAt < 0)
                    return name;

                try
                {
                    progress = GetLong(text.Substring(endAt + 1, progressEndAt - endAt - 1).Trim());
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex);
                    return name;
                }

                try
                {
                    size = GetLong(text.Substring(progressEndAt + 4, sizeEndAt - progressEndAt - 4).Trim());
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex);
                    return name;
                }

                return name;
            }

            private long GetLong(string text)
            {
                decimal multiplier = 1;

                if (string.IsNullOrEmpty(text))
                    return 0;

                if (text.EndsWith("kB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024;
                    text = text.Substring(0, text.Length - 2);
                }
                else if (text.EndsWith("MB", StringComparison.OrdinalIgnoreCase))
                {
                    multiplier = 1024 * 1024;
                    text = text.Substring(0, text.Length - 2);
                }
                else
                {
                    if (text.EndsWith("GB", StringComparison.OrdinalIgnoreCase))
                    {
                        multiplier = 1024 * 1024 * 1024;
                        text = text.Substring(0, text.Length - 2);
                    }
                    else
                    {
                        text = text.Substring(0, text.Length - 1);
                    }
                }

                // clean-up from some strange local chars:
                for (int i = 0; i < text.Length; i++)
                {
                    if (!char.IsDigit(text[i]) && text[i] != '.' && text[i] != ',')
                    {
                        text = text.Remove(i, 1);
                        i--;
                    }
                }

                // and convert the number finally:
                decimal value;
                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
                    return (long) (value * multiplier);

                if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                    return (long) (value * multiplier);

                return 0;
            }

            private static LogFetchFile Find(string text, IReadOnlyCollection<LogFetchFile> files)
            {
                if (string.IsNullOrEmpty(text) || files == null || files.Count == 0)
                    return null;

                foreach (var file in files)
                {
                    if (text.IndexOf(file.Name, StringComparison.CurrentCulture) >= 0)
                        return file;
                }

                return null;
            }

            private static LogFetchFile Get(string text, IReadOnlyCollection<LogFetchFile> files)
            {
                if (string.IsNullOrEmpty(text) || files == null || files.Count == 0)
                    return null;

                foreach (var file in files)
                {
                    if (string.Compare(text, file.Name, StringComparison.CurrentCulture) == 0)
                        return file;
                }

                return null;
            }

            private void Notify(string message)
            {
                Notify(message, -1, true);
            }

            private void Notify(string message, int progress, bool canAbort)
            {
                _parent.CanAbort = canAbort;

                var handler = Log;
                if (handler != null)
                    handler(_parent, new ApiLevelUpdateLogEventArgs(message, progress, canAbort));
            }

            #region Properties

            public LogState State
            {
                get { return _state; }
                private set
                {
                    if (_state != value)
                    {
                        _state = value;
                        TraceLog.WriteLine("New state: {0}", State);
                    }
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Event fired each time progress of an update has been changed.
        /// </summary>
        public event EventHandler<ApiLevelUpdateLogEventArgs> Log
        {
            add { _parser.Log += value; }
            remove { _parser.Log -= value; }
        }

        private readonly List<int> _runningInstallers;
        private LogParser _parser;

        private ApiLevelAction _action;
        private ApiLevelTarget _target;
        private Version _version;

        public ApiLevelUpdateRunner(string workingDirectory, ApiLevelAction action, ApiLevelTarget target, Version version)
            : base("cmd.exe", workingDirectory)
        {
            if (version == null)
                throw new ArgumentNullException("version");

            _action = action;
            _target = target;
            _version = version;
            _runningInstallers = new List<int>();
            _parser = new LogParser(this);
            CanAbort = true;

            UpdateArguments();
        }

        #region Properties

        /// <summary>
        /// Gets or sets the action to be performed.
        /// </summary>
        public ApiLevelAction Action
        {
            get { return _action; }
            set
            {
                _action = value;
                UpdateArguments();
            }
        }

        /// <summary>
        /// Gets or sets the target the action is performed over.
        /// </summary>
        public ApiLevelTarget Target
        {
            get { return _target; }
            set
            {
                _target = value;
                UpdateArguments();
            }
        }

        /// <summary>
        /// Gets or sets the version of the target operation is performed over.
        /// </summary>
        public Version Version
        {
            get { return _version; }
            set
            {
                if (value != null)
                {
                    _version = value;
                    UpdateArguments();
                }
            }
        }

        /// <summary>
        /// Gets an indication, if this task entered a moment, when can not be aborted.
        /// </summary>
        public bool CanAbort
        {
            get;
            private set;
        }

        #endregion

        private void UpdateArguments()
        {
            var args = new StringBuilder("/C eclipsec.exe");

            switch (Action)
            {
                case ApiLevelAction.Install:
                    args.Append(" --install");
                    break;
                case ApiLevelAction.Uninstall:
                    args.Append(" --uninstall");
                    break;
                default:
                    throw new InvalidOperationException("Specified action is unsupported (" + Action + ")");
            }
            args.Append(' ').Append(Version);

            switch (Target)
            {
                case ApiLevelTarget.NDK:
                    // do nothing
                    break;
                case ApiLevelTarget.Simulator:
                    args.Append(" --simulator");
                    break;
                case ApiLevelTarget.Runtime:
                    args.Append(" --runtime");
                    break;
                default:
                    throw new InvalidOperationException("Specified target is unsupported (" + Target + ")");
            }

            Arguments = args.ToString();
        }

        protected override void PrepareStartup()
        {
            base.PrepareStartup();
            _runningInstallers.Clear();
            _parser.Reset();
        }

        protected override void PrepareStarted(int pid)
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ParentProcessId=" + pid);
            foreach (var item in searcher.Get())
            {
                try
                {
                    _runningInstallers.Add(Convert.ToInt32(item["ProcessId"].ToString()));
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to acquire installation process-id");
                }
            }

        }

        protected override void ProcessOutputLine(string text)
        {
            _parser.Consume(text);
        }

        protected override void ConsumeResults(string output, string error)
        {
            _runningInstallers.Clear();
        }

        /// <summary>
        /// Waits for update to complete.
        /// </summary>
        public void Wait()
        {
            foreach (int pid in _runningInstallers)
            {
                using (var process = Process.GetProcessById(pid))
                {
                    process.WaitForExit();
                }
            }
        }

        /// <summary>
        /// Aborts running update.
        /// </summary>
        public bool Abort()
        {
            if (!CanAbort)
                return false;

            foreach (int pid in _runningInstallers)
            {
                try
                {
                    using (var process = Process.GetProcessById(pid))
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to abort update ({0}, {1}, {2})", Action, Target, Version);
                }
            }

            _runningInstallers.Clear();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Abort(); // kill all spawned processes if available...
        }
    }
}
