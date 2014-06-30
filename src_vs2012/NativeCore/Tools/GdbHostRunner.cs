using System;
using System.Diagnostics;
using System.Threading;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB via GDBHostprocess and helps in passing commands in both directions.
    /// </summary>
    public sealed class GdbHostRunner : ToolRunner, IGdbSender
    {
        private EventWaitHandle _eventCtrlC;
        private EventWaitHandle _eventTerminate;
        private GdbProcessor _processor;

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
            _processor = new GdbProcessor(this);

            // GDB-Host process required a specific order of arguments:
            // 1. the name of the event, which set will trigger the Ctrl+C signal to the GDB
            // 2. the name of the event, which set will exit the host process and GDB
            // 3. the path to GDB executable itself, that will run
            // 4. all the other arguments that should be passed to GDB (although it's possible to pass arguments to GDB via the executable path,
            //    but in practice they can't be escaped this way; that's why passing them as last arguments of the host are the recommended approach)
            Arguments = string.Concat(eventCtrlCName, " ", eventTerminateName, " ", "\"", gdb.Executable, "\" ", gdb.Arguments);
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
            if (_eventCtrlC == null)
                throw new ObjectDisposedException("GdbHostRunner");

            _eventCtrlC.Set();
        }

        /// <summary>
        /// Requests host process to terminate gracefully.
        /// </summary>
        public void Terminate()
        {
            if (_eventTerminate == null)
                throw new ObjectDisposedException("GdbHostRunner");

            _eventTerminate.Set();
        }

        /// <summary>
        /// Aborts the process and all its child processes.
        /// </summary>
        public override bool Abort()
        {
            if (_eventTerminate != null)
            {
                Terminate();
                return true;
            }

            return false;
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

                if (_processor != null)
                {
                    _processor.Dispose();
                    _processor = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Message Processing

        protected override void ProcessOutputLine(string text)
        {
            if (_processor != null)
            {
                _processor.Receive(text);
            }
        }

        #endregion

        #region IGdbSender Implementation

        void IGdbSender.Break()
        {
            Break();
        }

        void IGdbSender.Send(string text)
        {
            SendInput(text);
        }

        #endregion
    }
}
