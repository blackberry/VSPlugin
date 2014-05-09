using System;
using System.Diagnostics;
using System.Text;

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
            catch (Exception e)
            {
                Debug.WriteLine(_process.StartInfo.Arguments);
                Debug.WriteLine(e.Message);

                _process.Close();
            }
        }

        private void AsyncProcessExited(object sender, EventArgs e)
        {
            _process.Exited -= AsyncProcessExited;
            CompleteExecution();
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

            _output = null;
            _error = null;
            LastOutput = outputText;
            LastError = errorText;

            // release process resources:
            _process.Close();

            // consume received data:
            ConsumeResults(outputText, errorText);

            // notify other listeners, in case they want to get something extra:
            var finishedHandler = Finished;
            if (finishedHandler != null)
            {
                finishedHandler(this, new ToolRunnerEventArgs(outputText, errorText));
            }

            _isProcessing = false;
        }

        protected virtual void ConsumeResults(string output, string error)
        {
            // do nothing, subclasses should handle parsing output
        }
    }
}
