using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceControl : TargetService
    {
        public TargetServiceControl(Version version, IQConnReader source)
            : base("cntl", version, source)
        {
        }

        public override string ToString()
        {
            return "ControlService";
        }
    }
}
