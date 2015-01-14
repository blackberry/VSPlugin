using System;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    /// <summary>
    /// Class to communicate with a Control Service running on target.
    /// It is supposed to manage running processes.
    /// </summary>
    public sealed class TargetServiceControl : TargetService
    {
        private const int SIGKILL = 9;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetServiceControl(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        public override string ToString()
        {
            return "ControlService";
        }

        /// <summary>
        /// Terminates a process with specified PID.
        /// </summary>
        public void Terminate(uint pid)
        {
            Kill(pid, SIGKILL);
        }

        /// <summary>
        /// Terminates specified process.
        /// </summary>
        public void Terminate(ProcessInfo process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            Kill(process.ID, SIGKILL);
        }

        /// <summary>
        /// Terminates specified process.
        /// </summary>
        public void Terminate(TargetProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            Kill(process.PID, SIGKILL);
        }

        /// <summary>
        /// Sends a kill signal to a process with specified PID.
        /// </summary>
        public void Kill(uint pid, uint signal)
        {
            var response = Connection.Send(string.Concat("kill ", pid, " ", signal));
            QTraceLog.WriteLine("Killed process with PID: {0} - {1}", pid, response);
        }
    }
}
