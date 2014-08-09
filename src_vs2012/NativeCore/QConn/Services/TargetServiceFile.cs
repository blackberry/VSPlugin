using System;

namespace BlackBerry.NativeCore.QConn.Services
{
    public sealed class TargetServiceFile : TargetService
    {
        public TargetServiceFile(Version version, IQConnReader source)
            : base("file", version, source)
        {
        }

        public override string ToString()
        {
            return "FileService";
        }
    }
}
