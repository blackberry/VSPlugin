using System;
using System.Diagnostics;
using System.Threading;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB via GDBHostprocess and helps in passing commands in both directions.
    /// </summary>
    public sealed class GdbHostRunner : GdbRunner
    {
        private EventWaitHandle _eventCtrlC;
        private EventWaitHandle _eventTerminate;

        private static int HostID = 1;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="gdbHostFileName">Path to the BlackBerry.GDBHost.exe tool.</param>
        /// <param name="gdb">Description of the GDB to communicate with.</param>
        public GdbHostRunner(string gdbHostFileName, GdbInfo gdb)
            : base(gdbHostFileName, gdb)
        {
            int currentPID = Process.GetCurrentProcess().Id;
            int currentHostID = Interlocked.Increment(ref HostID);
            string eventCtrlCName = string.Concat("HostCtrlC-", currentHostID, "-", currentPID);
            string eventTerminateName = string.Concat("HostTerminate-", currentHostID, "-", currentPID);

            _eventCtrlC = new EventWaitHandle(false, EventResetMode.AutoReset, eventCtrlCName);
            _eventTerminate = new EventWaitHandle(false, EventResetMode.AutoReset, eventTerminateName);

            // GDB-Host process required a specific order of arguments:
            // 1. the name of the event, which set will trigger the Ctrl+C signal to the GDB
            // 2. the name of the event, which set will exit the host process and GDB
            // 3. the path to GDB executable itself, that will run
            // 4. optional settings for GDBHost (-s => disable custom console logs, -c => skip checking for GDB-executable existence)
            // 5. all the other arguments that should be passed to GDB (although it's possible to pass arguments to GDB via the executable path,
            //    but in practice they can't be escaped this way; that's why passing them as last arguments of the host are the recommended approach)
            Arguments = string.Concat(eventCtrlCName, " ", eventTerminateName, " -sc ", "\"", gdb.Executable, "\" ", gdb.Arguments);
        }

        /// <summary>
        /// Sends Ctrl+C to the GDB process.
        /// </summary>
        public override void Break()
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
            if (Processor != null)
            {
                try
                {
                    var request = RequestsFactory.Exit();
                    if (Processor.Send(request))
                    {
                        // synchronously wait for the completion:
                        request.Wait();
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to send input data");
                }
            }

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
            }
            base.Dispose(disposing);
        }

        #region Message Processing

        #endregion
    }
}
