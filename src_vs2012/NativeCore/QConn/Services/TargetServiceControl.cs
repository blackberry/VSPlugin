using System;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceControl : TargetService
    {
        private const int SIGKILL = 9;

        public TargetServiceControl(Version version, IQConnReader source)
            : base("cntl", version, source)
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
            Select();
            var response = Command(string.Concat("kill ", pid, " ", signal));
        }
    }
}
