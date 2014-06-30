using System.IO;
using BlackBerry.NativeCore;
using BlackBerry.NativeCore.Model;

namespace UnitTests
{
    internal static class Defaults
    {
        public const string IP = "10.0.0.147";
        public const string Password = "test";

        public static readonly string CertificatePath;
        public const string CertificatePassword = "abcd";

        public const string ToolsDirectory = @"S:\vs-plugin\qnxtools\bin";
        public static readonly string NdkDirectory;

        public const string InstalledNdkPath = @"C:\bbndk\10_2_2674";
        public const string GdbHostPath = @"S:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
        public static readonly string SshPublicKeyPath;
        public static readonly string DebugTokenPath;

        static Defaults()
        {
            NdkDirectory = ConfigDefaults.NdkDirectory;
            CertificatePath = Path.Combine(ConfigDefaults.DataDirectory, DeveloperDefinition.DefaultCertificateName);
            SshPublicKeyPath = ConfigDefaults.SshPublicKeyPath;
            DebugTokenPath = ConfigDefaults.DataFileName("debugtoken.bar");
        }
    }
}
