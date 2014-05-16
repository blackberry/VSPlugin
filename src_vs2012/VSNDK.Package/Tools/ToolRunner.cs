using System;
using System.Diagnostics;
using System.Text;
using RIM.VSNDK_Package.Diagnostics;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Class that runs specified executable, captures its output and error messages, then provides to parsers.
    /// </summary>
    internal class ToolRunner
    {
        private readonly Process _process;
        private StringBuilder _output;
        private StringBuilder _error;
        private bool _isProcessing;

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

        public string Arguments
        {
            get { return _process.StartInfo.Arguments; }
            set { _process.StartInfo.Arguments = value; }
        }

        public bool IsProcessing
        {
            get { return _isProcessing; } // using custom variable instead of _process.HasExited, as 'data processing' after tool termination should also indicate that state
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

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                _output.AppendLine(e.Data);
            }
        }

        private void ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                _error.AppendLine(e.Data);
            }
        }

        public bool Execute()
        {
            if (_isProcessing)
                throw new InvalidOperationException("The process is already running");
            if (string.IsNullOrEmpty(FileName))
                throw new InvalidOperationException("No executable to start");

            PrepareExecution();

            try
            {
                _process.EnableRaisingEvents = false;
                _process.Start();

                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
                _process.WaitForExit();

                return _process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Debug.WriteLine(_process.StartInfo.Arguments);
                Debug.WriteLine(e.Message);
                return false;
            }
            finally
            {
                CompleteExecution();
            }
        }

        public void ExecuteAsync()
        {
            if (_isProcessing)
                throw new InvalidOperationException("The process is already running");

            PrepareExecution();

            try
            {
                _process.Exited += AsyncProcessExited;
                _process.EnableRaisingEvents = true;

                _process.Start();
                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(_process.StartInfo.Arguments);
                TraceLog.WriteException(ex, "Unable to start {0}", GetType().Name);

                _process.Close();
                NotifyFinished(-1, null, null);
            }
        }

        private void AsyncProcessExited(object sender, EventArgs e)
        {
            _process.Exited -= AsyncProcessExited;
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
                    Dispatcher.Invoke(finishedHandler, this, new ToolRunnerEventArgs(exitCode, output, error));
                }
                else
                {
                    finishedHandler(this, new ToolRunnerEventArgs(exitCode, output, error));
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
        }

        private void CompleteExecution()
        {
            var outputText = _output.Length > 0 ? _output.ToString() : null;
            var errorText = _error.Length > 0 ? _error.ToString() : null;
            var exitCode = _process.ExitCode;

            _output = null;
            _error = null;
            LastOutput = outputText;
            LastError = errorText;

            // release process resources:
            _process.Close();

#if DEBUG
            // print received data:
            if (!string.IsNullOrEmpty(outputText))
                TraceLog.WriteLine(outputText.Trim());
            if (!string.IsNullOrEmpty(errorText))
                TraceLog.WarnLine(errorText.Trim());
#endif
            // consume received data:
            ConsumeResults(outputText, errorText);

            // notify other listeners, in case they want to get something extra:
            NotifyFinished(exitCode, outputText, errorText);

            _isProcessing = false;
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
    }
}
