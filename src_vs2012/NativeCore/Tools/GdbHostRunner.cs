using System;
using System.Diagnostics;
using System.Threading;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB via GDBHostprocess and helps in passing commands in both directions.
    /// </summary>
    public sealed class GdbHostRunner : ToolRunner
    {
        private EventWaitHandle _eventCtrlC;
        private EventWaitHandle _eventTerminate;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="gdbHostFileName">Path to the BlackBerry.GDBHost.exe tool.</param>
        /// <param name="gdb">Description of the GDB to communicate with.</param>
        public GdbHostRunner(string gdbHostFileName, GdbInfo gdb)
            : base(gdbHostFileName, null)
        {
            if (string.IsNullOrEmpty(gdbHostFileName))
                throw new ArgumentNullException("gdbHostFileName");
            if (gdb == null)
                throw new ArgumentNullException("gdb");

            GDB = gdb;

            int currentPID = Process.GetCurrentProcess().Id;
            string eventCtrlCName = "Ctrl-C-" + currentPID;
            string eventTerminateName = "Terminate-" + currentPID;
            _eventCtrlC = new EventWaitHandle(false, EventResetMode.AutoReset, eventCtrlCName);
            _eventTerminate = new EventWaitHandle(false, EventResetMode.AutoReset, eventTerminateName);

            Arguments = string.Concat("\"", gdb.Executable, "\" ", eventCtrlCName, " ", eventTerminateName, " ", gdb.Arguments);
        }

        #region Properties

        public GdbInfo GDB
        {
            get;
            private set;
        }

        public bool ShowConsole
        {
            get { return ShowWindow; }
            set { ShowWindow = value; }
        }

        #endregion

        /// <summary>
        /// Sends Ctrl+C to the GDB process.
        /// </summary>
        public void Break()
        {
            _eventCtrlC.Set();
        }

        /// <summary>
        /// Requests host process to terminate gracefully.
        /// </summary>
        public void Terminate()
        {
            _eventTerminate.Set();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_eventCtrlC != null)
                {
                    _eventCtrlC.Dispose();
                    _eventCtrlC = null;
                }

                if (_eventTerminate != null)
                {
                    _eventTerminate.Dispose();
                    _eventTerminate = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
