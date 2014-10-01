using System;
using System.IO;
using Microsoft.Win32;

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

        private const string FieldQnxToolsPath = "QNXToolsPath";
        private const string FieldVsNdkPath = "VSNDKPath";
        private const string FieldJavaHomePath = "JavaHomePath";

        /// <summary>
        /// Plugin-owned installation cache config directory.
        /// </summary>
        public static readonly string PluginInstallationConfigDirectory;
        public static readonly string RegistryPath;

#if DEBUG
        /// <summary>
        /// Full path to the DebugEngine library in build in debug, to help in easy debuggin whole stack.
        /// It will not be used in release builds, as then the assumption is that the DE is next to the package.
        /// </summary>
        public const string DebugEngineDebugablePath =
#               if PLATFORM_VS2010
                    @"T:\vs-plugin\src_vs2010\DebugEngine\bin\Debug\BlackBerry.DebugEngine.dll";
#               elif PLATFORM_VS2012
                    @"T:\vs-plugin\src_vs2012\DebugEngine\bin\Debug\BlackBerry.DebugEngine.dll";
#               elif PLATFORM_VS2013
                    @"T:\vs-plugin\src_vs2013\DebugEngine\bin\Debug\BlackBerry.DebugEngine.dll";
#               else
#                   error Define path to debug version of the DebugEngine.dll to make the debugging working.
#               endif
#endif

        static ConfigDefaults()
        {
            RegistryPath = @"Software\BlackBerry\VSPlugin";

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // read master values from registry:
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            string defaultToolsDirectory = null;
            string defaultNdkDirectory = null;
            string defaultJavaHomeDirectory = null;

            try
            {
                settings = registry.OpenSubKey(RegistryPath);
                if (settings != null)
                {
                    defaultToolsDirectory = (string) settings.GetValue(FieldQnxToolsPath);
                    defaultNdkDirectory = (string) settings.GetValue(FieldVsNdkPath);
                    defaultJavaHomeDirectory = (string) settings.GetValue(FieldJavaHomePath);
                }
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }

            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(programFilesX86))
                programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            ToolsDirectory = !string.IsNullOrEmpty(defaultToolsDirectory) ? defaultToolsDirectory : Path.Combine(programFilesX86, "BlackBerry", "VSPlugin-NDK", "qnxtools", "bin");
            NdkDirectory = !string.IsNullOrEmpty(defaultNdkDirectory) ? defaultNdkDirectory : Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs");
            JavaHome = FindJavaHomePath(defaultJavaHomeDirectory, NdkDirectory);

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
            SupplementaryInstallationConfigDirectory = Path.Combine(NdkDirectory, "..", "qconfig");
            SupplementaryPlayBookInstallationConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "QNX Software Systems", "qconfig");
            PluginInstallationConfigDirectory = Path.Combine(DataDirectory, "VSPlugin", "qconfig");

            SshPublicKeyPath = Path.Combine(DataDirectory, "bbt_id_rsa.pub");
            BuildDebugNativePath = Path.Combine(DataDirectory, "vsndk-debugNative.txt");

#if DEBUG
            // PH: INFO: here is a small trick - since we develop this plugin in 'debug build', we also overwrite in that build
            //           the place, where the debug-engine is stored (check package attributes), to have it also debuggable; that's why
            //           the package and DE are not in the same folder and even GDBHost is not inside any of them
            //           so let it be hardcoded for that moment to have a fluent GDB attaching...
#           if PLATFORM_VS2010
                GdbHostPath = @"T:\vs-plugin\src_vs2010\Debug\BlackBerry.GDBHost.exe";
#           elif PLATFORM_VS2012
                GdbHostPath = @"T:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
#           elif PLATFORM_VS2013
                GdbHostPath = @"T:\vs-plugin\src_vs2013\Debug\BlackBerry.GDBHost.exe";
#           endif
#else
            GdbHostPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "BlackBerry.GDBHost.exe");
#endif
        }

        #region Java Localization

        private static string FindJavaHomePath(string expectedJavaHomeDirectory, string vsndkDirectory)
        {
            // first check, if specified expected path is really a home of Java:
            if (!string.IsNullOrEmpty(expectedJavaHomeDirectory))
            {
                if (IsValidJavaHomePath(expectedJavaHomeDirectory))
                {
                    return expectedJavaHomeDirectory;
                }

                // nope, so maybe the parent directory has info about Java?
                var parentOfExpected = Path.GetDirectoryName(expectedJavaHomeDirectory);
                if (IsValidJavaHomePath(parentOfExpected))
                {
                    SetJavaHome(parentOfExpected);
                    return parentOfExpected;
                }
            }

            // is Java in any location of the BB-NDK for Visual Studio?
            if (!string.IsNullOrEmpty(vsndkDirectory))
            {
                // predefined one:
                var predefinedPath = Path.Combine(vsndkDirectory, "features", "com.qnx.tools.jre.win32_1.6.0.43", "jre");
                if (IsValidJavaHomePath(predefinedPath))
                {
                    SetJavaHome(null); // delete the value from registry, as we use the default one
                    return predefinedPath;
                }

                // or anywhere:
                var newPath = GetJavaHomeFromNdk(vsndkDirectory);
                if (!string.IsNullOrEmpty(newPath))
                {
                    SetJavaHome(newPath);
                    return newPath;
                }
            }

            // no way to find, where the Java is:
            return null;
        }

        private static bool IsValidJavaHomePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            return File.Exists(Path.Combine(path, "bin", "java.exe"));
        }

        /// <summary>
        /// Enumerates all folders from specified one looking for Java binaries.
        /// </summary>
        private static string GetJavaHomeFromNdk(string vsndkDirectory)
        {
            if (string.IsNullOrEmpty(vsndkDirectory))
                return null;

            // or if not, enumerate all folders (2-levels only) and finding one with 'java.exe' inside:
            try
            {
                foreach (var directory in Directory.EnumerateDirectories(vsndkDirectory))
                {
                    foreach (var feature in Directory.EnumerateDirectories(directory))
                    {
                        var expectedPath = Path.Combine(feature, "jre");
                        if (File.Exists(Path.Combine(expectedPath, "bin", "java.exe")))
                        {
                            return expectedPath;
                        }
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        public static void SetJavaHome(string path)
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RegistryPath);
                if (settings != null)
                {
                    if (!IsValidJavaHomePath(path))
                    {
                        settings.DeleteValue(FieldJavaHomePath, false);
                    }
                    else
                    {
                        settings.SetValue(FieldJavaHomePath, path, RegistryValueKind.String);
                    }
                }
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
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
