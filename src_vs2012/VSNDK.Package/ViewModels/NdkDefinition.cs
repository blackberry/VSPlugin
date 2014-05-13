using System;
using System.IO;
using Microsoft.Win32;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// Short definition of the NDK used during development.
    /// </summary>
    internal sealed class NdkDefinition
    {
        private const string FieldHostPath = "NDKHostPath";
        private const string FieldTargetPath = "NDKTargetPath";

        #region Properties

        public NdkDefinition(string hostPath, string targetPath)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            HostPath = hostPath;
            TargetPath = targetPath;
        }

        public string HostPath
        {
            get;
            private set;
        }

        public string TargetPath
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Retrieves info about used NDK from registry.
        /// </summary>
        public static NdkDefinition LoadInfo()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings;

            string hostPath = null;
            string targetPath = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
            }
            catch
            {
                // no pluging info found at all...
                return null;
            }
            if (settings == null)
                return null;

            try
            {
                var xHostPath = settings.GetValue(FieldHostPath);
                if (xHostPath != null)
                    hostPath = xHostPath.ToString();
            }
            catch
            {
            }

            try
            {
                var xTargetPath = settings.GetValue(FieldTargetPath);
                if (xTargetPath != null)
                    targetPath = xTargetPath.ToString();
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            // verify arguments:
            if (string.IsNullOrEmpty(hostPath) || string.IsNullOrEmpty(targetPath))
                return null;

            return new NdkDefinition(hostPath, targetPath);
        }

        /// <summary>
        /// Saves NDK info into the registry.
        /// </summary>
        public void SaveInfo()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.SetValue(FieldHostPath, HostPath);
                settings.SetValue(FieldTargetPath, TargetPath);

                string qnxConfigPath = Path.Combine(RunnerDefaults.DataDirectory, "BlackBerry Native SDK");

                // TODO: PH: copied from 'SettingsData.cs' - I don't quite get, why we need to overwrite EV
                // as this will break Android as well as installed BB10 NDKs with Momentics...
                Environment.SetEnvironmentVariable("QNX_TARGET", TargetPath);
                Environment.SetEnvironmentVariable("QNX_HOST", HostPath);
                Environment.SetEnvironmentVariable("QNX_CONFIGURATION", qnxConfigPath);

                string ndkpath = string.Format(@"{0}/usr/bin;{1}\bin;{0}/usr/qde/eclipse/jre/bin;", HostPath, qnxConfigPath) + Environment.GetEnvironmentVariable("PATH");
                Environment.SetEnvironmentVariable("PATH", ndkpath);
            }
            catch
            {
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }
    }
}
