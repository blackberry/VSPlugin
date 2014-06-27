using BlackBerry.NativeCore;

namespace UnitTests
{
    internal static class Defaults
    {
        public const string IP = "10.0.0.147";
        public const string Password = "test";

        public static readonly string ToolsDirectory;
        public static readonly string NdkDirectory;

        public const string InstalledNdkPath = @"C:\bbndk\10_2_2674";
        public static readonly string SshPublicKeyPath;
        public static readonly string DebugTokenPath;

        static Defaults()
        {
            // TODO: PH: 2014-05-08: for now hardcoded my repository path:
            ToolsDirectory = @"S:\vs-plugin\qnxtools\bin";
            NdkDirectory = ConfigDefaults.NdkDirectory; // @"S:\vs-plugin\bbndk_vs";

            SshPublicKeyPath = ConfigDefaults.SshPublicKeyPath;
            DebugTokenPath = ConfigDefaults.DataFileName("debugtoken.bar");
        }
    }
}
