using System;
using System.IO;

namespace RIM.VSNDK_Package.Tools
{
    internal static class RunnerDefaults
    {
        public static readonly string TestToolsDirectory;
        public static readonly string TestNdkDirectory;

        public static readonly string ToolsDirectory;
        public static readonly string NdkDirectory;
        public static readonly string DataDirectory;
        public static readonly string InstallationConfigDirectory;
        public static readonly string SupplementaryInstallationConfigDirectory;
        public static readonly string RegistryPath;

        static RunnerDefaults()
        {
            ToolsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "BlackBerry", "VSPlugin-NDK", "qnxtools", "bin");
            NdkDirectory = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs");
            DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Research In Motion");
            InstallationConfigDirectory = Path.Combine(DataDirectory, "BlackBerry Native SDK", "qconfig");
            SupplementaryInstallationConfigDirectory = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs", "..", "qconfig");
            RegistryPath = @"Software\BlackBerry\BlackBerryVSPlugin";

#if DEBUG
            // TODO: PH: 2014-05-08: for now hardcoded my repository path:
            TestToolsDirectory = @"S:\vs-plugin\qnxtools\bin";
            TestNdkDirectory = @"S:\vs-plugin\bbndk_vs";
#endif
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
    }
}
