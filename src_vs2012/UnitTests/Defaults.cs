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

        public const string ToolsDirectory = @"T:\vs-plugin\qnxtools\bin";
        public static readonly string NdkDirectory;

        public const string InstalledNdkPath = @"C:\bbndk\10_2_2674";
        public const string InstalledRuntimePath = @"C:\bbndk_vs\runtime_10_2_1_3247\qnx6";
        public const string GdbHostPath = @"T:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
        public const string SampleCascadesProjectPath = @"T:\bb_samples\NDK-Samples\";
        public const string SampleOpenGlesProjectPath = @"T:\bb_samples\OpenGLES-Samples\OpenGLES2-ProgrammingGuide";

        public static readonly string SshPublicKeyPath;
        public static readonly string SshPrivateKeyPath;
        public static readonly string DebugTokenPath;

        static Defaults()
        {
            NdkDirectory = ConfigDefaults.NdkDirectory;
            CertificatePath = Path.Combine(ConfigDefaults.DataDirectory, DeveloperDefinition.DefaultCertificateName);
            SshPublicKeyPath = ConfigDefaults.SshPublicKeyPath;
            SshPrivateKeyPath = ConfigDefaults.SshPrivateKeyPath;
            DebugTokenPath = ConfigDefaults.DataFileName("debugtoken.bar");
        }
    }
}
