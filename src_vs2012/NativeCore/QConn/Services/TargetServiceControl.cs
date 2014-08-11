using System;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceControl : TargetService
    {
        private const int SIGKILL = 9;

        public TargetServiceControl(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        public override string ToString()
        {
            return "ControlService";
        }

        public void Kill(uint pid)
        {
            Kill(pid, SIGKILL);
        }

        public void Kill(SystemInfoProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            Kill(process.ID, SIGKILL);
        }

        public void Kill(uint pid, uint signal)
        {
            var response = Connection.Send(string.Concat("kill ", pid, " ", signal));
            QTraceLog.WriteLine("Killed process with PID: {0} - {1}", pid, response);
        }
    }
}
