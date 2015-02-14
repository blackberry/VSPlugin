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
        /// <summary>
        /// Global version of the plugin. It is set as a property of all projects.
        /// </summary>
        public const string VersionString = "3.0.0";

        public static string ToolsDirectory
        {
            get;
            private set;
        }

        public static string NdkDirectory
        {
            get;
            private set;
        }

        public static string JavaHome
        {
            get;
            private set;
        }

        public static string SupplementaryInstallationConfigDirectory
        {
            get;
            private set;
        }

        public static readonly string DataDirectory;
        public static readonly string InstallationConfigDirectory;
        public static readonly string SupplementaryPlayBookInstallationConfigDirectory;
        public static readonly string SshPublicKeyPath;
        public static readonly string SshPrivateKeyPath;
        public static readonly string GdbHostPath;

        private const string FieldQnxToolsPath = "QNXToolsPath";
        private const string FieldVsNdkPath = "VSNDKPath";
        private const string FieldVsNdkConfigPath = "VSNDKConfigPath";
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

        /// <summary>
        /// Gets the path to version of MSBuild used by Visual Studio.
        /// </summary>
        public static string MSBuildVCTargetsPath
        {
            get
            {
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                if (string.IsNullOrEmpty(programFilesX86))
                {
                    programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                }

#if PLATFORM_VS2010
                return Path.Combine(programFilesX86, "MSBuild", "Microsoft.Cpp", "v4.0");
#elif PLATFORM_VS2012
                return Path.Combine(programFilesX86, "MSBuild", "Microsoft.Cpp", "v4.0", "V110");
#elif PLATFORM_VS2013
                return Path.Combine(programFilesX86, "MSBuild", "Microsoft.Cpp", "v4.0", "V120");
#else
#               error Define MSBuild path for current version of Visual Studio.
#endif
            }
        }

        /// <summary>
        /// Get the path to Wiki pages.
        /// </summary>
        public static string GithubProjectWikiInstallation
        {
            get { return "https://github.com/phofman/vs-plugin/wiki/Installation"; }
        }

        public static string GithubProjectSourceCode
        {
            get { return "https://github.com/phofman/vs-plugin"; } // "https://github.com/blackberry/VSPlugin"
        }

        public static string GithubProjectIssues
        {
            get { return "https://github.com/phofman/vs-plugin/issues"; } // "https://github.com/blackberry/VSPlugin/issues"
        }

        public static string GithubProjectAuthor
        {
            get { return "http://www.codetitans.pl"; }
        }

        public static string GithubProjectAuthorTwitter
        {
            get { return "https://twitter.com/CodeTitans"; }
        }

        public static string GithubProjectReleaseBBNDKforVS
        {
            get { return "https://github.com/phofman/vs-plugin/releases/tag/v3.0.1-bbndk"; }
        }

        static ConfigDefaults()
        {
            RegistryPath = @"Software\BlackBerry\VSPlugin";

            LoadDynamicSettings();

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
            SupplementaryPlayBookInstallationConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "QNX Software Systems", "qconfig");
            PluginInstallationConfigDirectory = Path.Combine(DataDirectory, "VSPlugin", "qconfig");

            SshPublicKeyPath = Path.Combine(DataDirectory, "bbt_id_rsa.pub");
            SshPrivateKeyPath = Path.Combine(DataDirectory, "bbt_id_rsa");

#if DEBUG
            // PH: INFO: here is a small trick - since we develop this plugin in 'debug build', we also overwrite in that build
            //           the place, where the debug-engine is stored (check package attributes), to have it also debuggable; that's why
            //           the package and DE are not in the same folder and even GDBHost is not inside any of them
            //           so let it be hardcoded for that moment to have a fluent GDB attaching...
#           if PLATFORM_VS2010
                GdbHostPath = @"T:\vs-plugin\src_vs2010\GDBHost\Debug\BlackBerry.GDBHost.exe";
#           elif PLATFORM_VS2012
                GdbHostPath = @"T:\vs-plugin\src_vs2012\Debug\BlackBerry.GDBHost.exe";
#           elif PLATFORM_VS2013
                GdbHostPath = @"T:\vs-plugin\src_vs2013\Debug\BlackBerry.GDBHost.exe";
#           else
#               error Define path to debug version of the GDBHost.dll to make the debugging working.
#           endif
#else
            GdbHostPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "BlackBerry.GDBHost.exe");
#endif
        }

        private static void LoadDynamicSettings()
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // read master values from registry:
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            string defaultToolsDirectory = null;
            string defaultNdkDirectory = null;
            string defaultNdkConfigDirectory = null;
            string defaultJavaHomeDirectory = null;

            try
            {
                settings = registry.OpenSubKey(RegistryPath);
                if (settings != null)
                {
                    defaultToolsDirectory = (string) settings.GetValue(FieldQnxToolsPath);
                    defaultNdkDirectory = (string) settings.GetValue(FieldVsNdkPath);
                    defaultNdkConfigDirectory = (string) settings.GetValue(FieldVsNdkConfigPath);
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

            ToolsDirectory = !string.IsNullOrEmpty(defaultToolsDirectory) ? defaultToolsDirectory : GetEmbeddedToolsLocation();
            ToolsDirectory = ExistingPathOrNull(ToolsDirectory) ?? GetToolsDefaultLocation();

            NdkDirectory = !string.IsNullOrEmpty(defaultNdkDirectory) ? defaultNdkDirectory : GetNdkDefaultLocation();
            JavaHome = FindJavaHomePath(defaultJavaHomeDirectory, NdkDirectory) ?? GetOracleJavaDefaultLocation();
            SupplementaryInstallationConfigDirectory = !string.IsNullOrEmpty(defaultNdkConfigDirectory) ? defaultNdkConfigDirectory : Path.Combine(Path.GetDirectoryName(NdkDirectory), "qconfig");

            // make sure paths exist:
            ToolsDirectory = ExistingPathOrNull(ToolsDirectory);
            NdkDirectory = ExistingPathOrNull(NdkDirectory);
            JavaHome = ExistingPathOrNull(JavaHome);
            SupplementaryInstallationConfigDirectory = ExistingPathOrNull(SupplementaryInstallationConfigDirectory);
        }

        #region Default Locations

        /// <summary>
        /// Returns specified path, or null, when it doesn't exist.
        /// </summary>
        private static string ExistingPathOrNull(string path)
        {
            return Directory.Exists(path) ? path : null;
        }

        private static string GetToolsDefaultLocation()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (string.IsNullOrEmpty(programFilesX86))
                programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            return Path.Combine(programFilesX86, "MSBuild", "BlackBerry", "QnxTools", "bin");
        }

        private static string GetEmbeddedToolsLocation()
        {
            var executingAssemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = Path.Combine(Path.GetDirectoryName(executingAssemblyLocation), "QnxTools", "bin");

            // HACK: somehow the location is written in capitals, what doesn't look good on UI
            // so convert it to short path and back to long, to force to OS to provide the location
            // using the true folder names
            return NativeMethods.GetLongPathName(NativeMethods.GetShortPathName(path));
        }

        private static string GetNdkDefaultLocation()
        {
            return Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)), "bbndk_vs");
        }

        private static string GetJavaHomeDefaultLocation(string vsndkDirectory)
        {
            return Path.Combine(vsndkDirectory, "features", "com.qnx.tools.jre.win32_1.6.0.43", "jre");
        }

        private static string GetOracleJavaDefaultLocation()
        {
            var path = FindJavaHomePath(null, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            return !string.IsNullOrEmpty(path) ? path : FindJavaHomePath(null, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        }

        #endregion

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
                var predefinedPath = GetJavaHomeDefaultLocation(vsndkDirectory);
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
            if (string.IsNullOrEmpty(vsndkDirectory) || !Directory.Exists(vsndkDirectory))
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

        private static void SetJavaHome(string path)
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RegistryPath);
                SetOrDelete(settings, FieldJavaHomePath, IsValidJavaHomePath(path) ? path : null, GetJavaHomeDefaultLocation(NdkDirectory), null);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        #endregion

        public static void Apply(string vsndkDirectory, string toolsDirectory, string javaHomeDirectory)
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RegistryPath);
                SetOrDelete(settings, FieldVsNdkPath, vsndkDirectory, GetNdkDefaultLocation(), null);
                SetOrDelete(settings, FieldQnxToolsPath, toolsDirectory, GetEmbeddedToolsLocation(), GetToolsDefaultLocation());
                SetOrDelete(settings, FieldJavaHomePath, javaHomeDirectory, GetJavaHomeDefaultLocation(vsndkDirectory), null);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }

            LoadDynamicSettings();
        }

        private static void SetOrDelete(RegistryKey context, string fieldName, string value, string defaultValue1, string defaultValue2)
        {
            if (string.IsNullOrEmpty(fieldName))
                throw new ArgumentNullException("fieldName");

            if (context != null)
            {
                if (string.IsNullOrEmpty(value)
                    || string.Compare(value, defaultValue1, StringComparison.OrdinalIgnoreCase) == 0
                    || string.Compare(value, defaultValue2, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    context.DeleteValue(fieldName, false);
                }
                else
                {
                    context.SetValue(fieldName, value, RegistryValueKind.String);
                }
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
