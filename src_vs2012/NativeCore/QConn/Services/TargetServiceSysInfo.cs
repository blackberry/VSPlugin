using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceSysInfo : TargetService
    {
        public TargetServiceSysInfo(string host, int port, Version version)
            : base(host, port, "sinfo", version)
        {
        }

        public override string ToString()
        {
            return "SysInfoService";
        }
    }
}
