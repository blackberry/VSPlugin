using System;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Arguments passed along with 'Log' event.
    /// </summary>
    public class ApiLevelUpdateLogEventArgs : EventArgs
    {
        public ApiLevelUpdateLogEventArgs(string message, int progress, bool canAbort)
        {
            Message = message;
            Progress = progress;
            CanAbort = canAbort;
        }

        public ApiLevelUpdateLogEventArgs(string message)
        {
            Message = message;
            Progress = -1;
            CanAbort = true;
        }

        #region Properties

        public string Message
        {
            get;
            private set;
        }

        public int Progress
        {
            get;
            private set;
        }

        public bool CanAbort
        {
            get;
            private set;
        }

        #endregion
    }
}