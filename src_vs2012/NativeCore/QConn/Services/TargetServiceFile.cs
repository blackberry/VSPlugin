using System;
using BlackBerry.NativeCore.QConn.Model;

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

        public TargetFile Stat(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            var response = Connection.Send(string.Concat("oc:\"", path, "\""));

            return null;
        }
    }
}
