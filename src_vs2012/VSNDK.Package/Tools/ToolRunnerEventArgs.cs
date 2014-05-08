using System;

namespace RIM.VSNDK_Package.Tools
{
    /// <summary>
    /// Arguments passes along with ToolRunner events.
    /// </summary>
    internal sealed class ToolRunnerEventArgs : EventArgs
    {
        #region Properties

        public ToolRunnerEventArgs(string output, string error)
        {
            Output = output;
            Error = error;
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

        #endregion
    }
}
