using System;
using System.IO;

namespace RIM.VSNDK_Package.Tools
{
    internal static class RunnerDefaults
    {
        public static readonly string TestToolsDirectory;
        public static readonly string ConfigurationDirectory;

        static RunnerDefaults()
        {
            ConfigurationDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Research In Motion";

#if DEBUG
            // TODO: PH: 2014-05-08: for now hardcoded my repository path:
            TestToolsDirectory = @"S:\vs-plugin\qnxtools\bin";
#endif
        }

        /// <summary>
        /// Gets the full path to specified file within BlackBerry developer's configuration area.
        /// </summary>
        public static string ConfigFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            return Path.Combine(ConfigurationDirectory, fileName);
        }
    }
}
