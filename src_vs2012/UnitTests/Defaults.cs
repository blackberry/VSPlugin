using BlackBerry.NativeCore;

namespace UnitTests
{
    internal static class Defaults
    {
        public const string IP = "10.0.0.147";
        public const string Password = "test";

        public const string ToolsDirectory = @"S:\vs-plugin\qnxtools\bin";
        public static readonly string NdkDirectory;

        public const string InstalledNdkPath = @"C:\bbndk\10_2_1_653";
        public const string GdbHostPath = @"S:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
        public static readonly string SshPublicKeyPath;
        public static readonly string DebugTokenPath;

        static Defaults()
        {
            // TODO: PH: 2014-05-08: for now hardcoded my repository path:
            NdkDirectory = ConfigDefaults.NdkDirectory; // @"S:\vs-plugin\bbndk_vs";

            SshPublicKeyPath = ConfigDefaults.SshPublicKeyPath;
            DebugTokenPath = ConfigDefaults.DataFileName("debugtoken.bar");
        }
    }
}
