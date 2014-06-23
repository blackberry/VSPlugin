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
        public static NdkDefinition Load()
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
                    hostPath = Path.GetFullPath(xHostPath.ToString());
            }
            catch
            {
            }

            try
            {
                var xTargetPath = settings.GetValue(FieldTargetPath);
                if (xTargetPath != null)
                    targetPath = Path.GetFullPath(xTargetPath.ToString());
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
        /// Removes NDK info from the registry.
        /// </summary>
        public static void Delete()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteValue(FieldHostPath, false);
                settings.DeleteValue(FieldTargetPath, false);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Saves NDK info into the registry.
        /// </summary>
        public void Save()
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
