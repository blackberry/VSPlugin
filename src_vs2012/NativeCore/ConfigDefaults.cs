using System;
using System.IO;

namespace BlackBerry.NativeCore
{
#if MAKE_CONFIG_DEFAULTS_PUBLIC
    public
#endif
    static class ConfigDefaults
    {
        public static readonly string ToolsDirectory;
        public static readonly string NdkDirectory;
        public static readonly string DataDirectory;
        public static readonly string InstallationConfigDirectory;
        public static readonly string SupplementaryInstallationConfigDirectory;
        public static readonly string SupplementaryPlayBookInstallationConfigDirectory;

        public static readonly string JavaHome;
        public static readonly string SshPublicKeyPath;
        public static readonly string BuildDebugNativePath;
        public static readonly string GdbHostPath;


        /// <summary>
        /// Plugin-owned installation cache config directory.
        /// </summary>
        public static readonly string PluginInstallationConfigDirectory;
        public static readonly string RegistryPath;

        static ConfigDefaults()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(programFilesX86))
                programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            ToolsDirectory = Path.Combine(programFilesX86, "BlackBerry", "VSPlugin-NDK", "qnxtools", "bin");
            NdkDirectory = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs");
            // PH: TODO: this would probably be much safer, when enumerating all folders and finding one with 'java.exe' or 'jre' in the name
            JavaHome = Path.Combine(NdkDirectory, "features", "com.qnx.tools.jre.win32_1.6.0.43", "jre");

            // the base data folder is different for each platform...
            if (IsWindowsXP)
            {
                // HINT: in general LocalApplicationData should point to similar path...
                // but if you use localized version of Windows XP, the folder is different ;)
                DataDirectory = Path.Combine(Environment.ExpandEnvironmentVariables("%HomeDrive%%HomePath%"), "Local Settings", "Application Data", "Research In Motion");
            }
            else
            {
                DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Research In Motion");
            }

            InstallationConfigDirectory = Path.Combine(DataDirectory, "BlackBerry Native SDK", "qconfig");
            SupplementaryInstallationConfigDirectory = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs", "..", "qconfig");
            SupplementaryPlayBookInstallationConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "QNX Software Systems", "qconfig");
            PluginInstallationConfigDirectory = Path.Combine(DataDirectory, "VSPlugin", "qconfig");

            SshPublicKeyPath = Path.Combine(DataDirectory, "bbt_id_rsa.pub");
            BuildDebugNativePath = Path.Combine(DataDirectory, "vsndk-debugNative.txt");
            RegistryPath = @"Software\BlackBerry\BlackBerryVSPlugin";
            GdbHostPath = @"S:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
        }

        /// <summary>
        /// Gets the full path to specified file within BlackBerry developer's configuration area.
        /// </summary>
        public static string DataFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            return Path.Combine(DataDirectory, fileName);
        }

        /// <summary>
        /// Gets an indication, if currently running on Windows XP.
        /// </summary>
        public static bool IsWindowsXP
        {
            get
            {
                var os = Environment.OSVersion;
                return os.Platform == PlatformID.Win32NT && os.Version.Major == 5 && os.Version.Minor == 1;
            }
        }

        /// <summary>
        /// Gets an indication, if currently running on Windows XP, Vista, 7, 8 or newer system.
        /// </summary>
        public static bool IsWindowsXPorNewer
        {
            get
            {
                var os = Environment.OSVersion;
                return os.Platform == PlatformID.Win32NT && (os.Version.Major > 5 || (os.Version.Major == 5 && os.Version.Minor == 1));
            }
        }
    }
}
