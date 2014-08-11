using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceFile : TargetService
    {
        public TargetServiceFile(Version version, QConnConnection connection)
            : base(version, connection)
        {
        }

        public override string ToString()
        {
            return "FileService";
        }
    }
}
