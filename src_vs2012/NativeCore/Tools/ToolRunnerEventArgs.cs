using System;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Arguments passes along with ToolRunner events.
    /// </summary>
    public sealed class ToolRunnerEventArgs : EventArgs
    {
        #region Properties

        public ToolRunnerEventArgs(int exitCode, string output, string error, object tag)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
            Tag = tag;
        }

        public int ExitCode
        {
            get;
            private set;
        }

        public string Output
        {
            get;
            private set;
        }

        public string Error
        {
            get;
            private set;
        }

        public bool IsSuccessfull
        {
            get { return string.IsNullOrEmpty(Error); }
        }

        public object Tag
        {
            get;
            private set;
        }

        #endregion
    }
}
