using System;
using System.IO;

namespace RIM.VSNDK_Package.Tools
{
    internal static class RunnerDefaults
    {
        public static readonly string TestToolsDirectory;
        public static readonly string TestNdkDirectory;
        public static readonly string DataDirectory;
        public static readonly string InstallationConfigDirectory;

        static RunnerDefaults()
        {
            DataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion";
            InstallationConfigDirectory = Path.Combine(DataDirectory, @"BlackBerry Native SDK\qconfig");

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
