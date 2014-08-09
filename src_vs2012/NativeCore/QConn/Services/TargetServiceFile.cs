using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceFile : TargetService
    {
        public TargetServiceFile(string host, int port, Version version)
            : base(host, port, "file", version)
        {
        }

        public override string ToString()
        {
            return "FileService";
        }
    }
}
