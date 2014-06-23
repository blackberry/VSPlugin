using System;
using System.IO;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.Tools;
using Microsoft.Win32;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Short definition of the NDK used during development.
    /// </summary>
    public sealed class NdkDefinition
    {
        private const string FieldHostPath = "NDKHostPath";
        private const string FieldTargetPath = "NDKTargetPath";
        private const string FieldFamilyType = "NDKFamilyType";

        #region Properties

        public NdkDefinition(string hostPath, string targetPath, DeviceFamilyType type)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            HostPath = hostPath;
            TargetPath = targetPath;
            Type = type;
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

        public DeviceFamilyType Type
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
            DeviceFamilyType type = DeviceFamilyType.Phone;

            try
            {
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
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

            try
            {
                var xType = settings.GetValue(FieldFamilyType);
                if (xType != null)
                    type = DeviceFamilyHelper.GetTypeFromString(xType.ToString());
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            // verify arguments:
            if (string.IsNullOrEmpty(hostPath) || string.IsNullOrEmpty(targetPath))
                return null;

            return new NdkDefinition(hostPath, targetPath, type);
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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteValue(FieldHostPath, false);
                settings.DeleteValue(FieldTargetPath, false);
                settings.DeleteValue(FieldFamilyType, false);
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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.SetValue(FieldHostPath, HostPath);
                settings.SetValue(FieldTargetPath, TargetPath);
                settings.SetValue(FieldFamilyType, DeviceFamilyHelper.GetTypeToString(Type));
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
