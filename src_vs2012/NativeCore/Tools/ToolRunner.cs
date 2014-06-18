using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Class that runs specified executable, captures its output and error messages, then provides to parsers.
    /// </summary>
    public class ToolRunner : IDisposable
    {
        private Process _process;
        private StringBuilder _output;
        private StringBuilder _error;
        private bool _isProcessing;

        public static event EventHandler<ToolRunnerStartupEventArgs> Startup;
        public event EventHandler<ToolRunnerEventArgs> Finished;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ToolRunner()
        {
            _process = new Process();

            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;

            _process.OutputDataReceived += OutputDataReceived;
            _process.ErrorDataReceived += ErrorDataReceived;
        }

        ~ToolRunner()
        {
            Dispose(false);
        }

        /// <summary>
        /// Init constructor. Setups the executable and its working directory.
        /// </summary>
        /// <param name="fileName">Name of the binary to execute</param>
        /// <param name="workingDirectory">Executable working directory</param>
        public ToolRunner(string fileName, string workingDirectory)
            : this()
        {
            FileName = fileName;
            WorkingDirectory = workingDirectory;
        }

        #region Properties

        public IEventDispatcher Dispatcher
        {
            get;
            set;
        }

        public string FileName
        {
            get { return _process.StartInfo.FileName; }
            set { _process.StartInfo.FileName = value; }
        }

        public string WorkingDirectory
        {
            get { return _process.StartInfo.WorkingDirectory; }
            set { _process.StartInfo.WorkingDirectory = value; }
        }

        public StringDictionary Environment
        {
            get { return _process.StartInfo.EnvironmentVariables; }
        }

        public string Arguments
        {
            get { return _process.StartInfo.Arguments; }
            set { _process.StartInfo.Arguments = value; }
        }

        public bool IsProcessing
        {
            get { return _isProcessing; } // using custom variable instead of _process.HasExited, as 'data processing' after tool termination should also indicate that state
        }

        public int ExitCode
        {
            get;
            private set;
        }

        public string LastOutput
        {
            get;
            protected set;
        }

        public string LastError
        {
            get;
            protected set;
        }

        public object Tag
        {
            get;
            set;
        }

        #endregion

        protected virtual void ProcessOutputLine(string text)
        {
            if (_output != null && text != null)
                _output.AppendLine(text);
        }

        protected virtual void ProcessErrorLine(string text)
        {
            if (_error != null && text != null)
                _error.AppendLine(text);
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                ProcessOutputLine(e.Data);
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                ProcessErrorLine(e.Data);
            }
        }

        public bool Execute()
        {
            if (_isProcessing)
                throw new InvalidOperationException("The process is already running");
            if (_process == null)
                throw new ObjectDisposedException("ToolRunner");
            if (string.IsNullOrEmpty(FileName))
                throw new InvalidOperationException("No executable to start");

            PrepareExecution();
            ExitCode = int.MinValue;

            try
            {
                _process.EnableRaisingEvents = false;
                _process.Start();

                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();

                PrepareStarted(_process.Id);
                _process.WaitForExit();
                ExitCode = _process.ExitCode;

                return ExitCode == 0;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to start {0}", GetType().Name);
                ProcessErrorLine(ex.Message);
                return false;
            }
            finally
            {
                // release process resources:
                _process.Close();

                CompleteExecution();
            }
        }

        public void ExecuteAsync()
        {
            if (_isProcessing)
                throw new InvalidOperationException("The process is already running");
            if (_process == null)
                throw new ObjectDisposedException("ToolRunner");

            PrepareExecution();
            ExitCode = int.MinValue;

            try
            {
                _process.Exited += AsyncProcessExited;
                _process.EnableRaisingEvents = true;

                _process.Start();
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();

                PrepareStarted(_process.Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(_process.StartInfo.Arguments);
                TraceLog.WriteException(ex, "Unable to start {0}", GetType().Name);

                _process.Close();

                ProcessErrorLine(ex.Message);
                NotifyFinished(-1, null, null);
            }
        }

        private void AsyncProcessExited(object sender, EventArgs e)
        {
            ExitCode = _process.ExitCode;
            _process.Exited -= AsyncProcessExited;

            // release process resources:
            _process.Close();

            CompleteExecution();
        }

        private void NotifyFinished(int exitCode, string output, string error)
        {
            var finishedHandler = Finished;

            if (finishedHandler != null)
            {
                // perform a cross-thread notification (in case we want to update UI directly from the handler)
                if (Dispatcher != null)
                {
                    Dispatcher.Invoke(finishedHandler, this, new ToolRunnerEventArgs(exitCode, output, error, Tag));
                }
                else
                {
                    finishedHandler(this, new ToolRunnerEventArgs(exitCode, output, error, Tag));
                }
            }
        }

        private void PrepareExecution()
        {
            _isProcessing = true;
            LastOutput = null;
            LastError = null;
            _output = new StringBuilder();
            _error = new StringBuilder();

            // allow setup customization by sub-classes:
            PrepareStartup();
        }

        private void CompleteExecution()
        {
            var outputText = _output != null && _output.Length > 0 ? _output.ToString() : null;
            var errorText = _error != null && _error.Length > 0 ? _error.ToString() : null;

            _output = null;
            _error = null;
            LastOutput = outputText;
            LastError = errorText;

#if DEBUG
            TraceLog.WriteLine("{0} {1}", _process.StartInfo.FileName, _process.StartInfo.Arguments);
            TraceLog.WriteLine(" * PATH: {0}", _process.StartInfo.EnvironmentVariables["PATH"]);
            TraceLog.WriteLine(" * QNX_TARGET: {0}", _process.StartInfo.EnvironmentVariables["QNX_TARGET"]);
            TraceLog.WriteLine(" * QNX_HOST: {0}", _process.StartInfo.EnvironmentVariables["QNX_HOST"]);

            // print received data:
            if (!string.IsNullOrEmpty(outputText))
                TraceLog.WriteLine(outputText.Trim());
            if (!string.IsNullOrEmpty(errorText))
                TraceLog.WarnLine(errorText.Trim());
#endif

            // consume received data:
            ConsumeResults(outputText, errorText);

            // notify other listeners, in case they want to get something extra:
            NotifyFinished(ExitCode, outputText, errorText);

            _isProcessing = false;
        }

        /// <summary>
        /// Method executed before starting the tool, to setup the state of the current runner.
        /// </summary>
        protected virtual void PrepareStartup()
        {
            // this is the place to override and DISABLE running GLOBAL setup:
            var handler = Startup;
            if (handler != null)
                handler(this, new ToolRunnerStartupEventArgs(this));
        }

        /// <summary>
        /// Method executed just after staring the tool, to setup extra behavior of the runner.
        /// </summary>
        protected virtual void PrepareStarted(int pid)
        {
        }

        protected virtual void ConsumeResults(string output, string error)
        {
            // do nothing, subclasses should handle parsing output
        }

        /// <summary>
        /// Extracts the error messages out of the given text.
        /// </summary>
        protected static string ExtractErrorMessages(string error)
        {
            if (string.IsNullOrEmpty(error))
                return null;

            var result = new StringBuilder();
            var lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.AppendLine(line.Substring(6).Trim());
                }
            }

            return result.ToString();
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_process != null)
            {
                _process.OutputDataReceived -= OutputDataReceived;
                _process.ErrorDataReceived -= ErrorDataReceived;

                _process.Dispose();
                _process = null;
            }

            Finished = null;
        }

        #endregion
    }
}
