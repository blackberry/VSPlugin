using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceControl : TargetService
    {
        public TargetServiceControl(string host, int port, Version version)
            : base(host, port, "cntl", version)
        {
        }

        public override string ToString()
        {
            return "ControlService";
        }
    }
}
