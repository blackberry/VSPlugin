using System;
using Microsoft.Win32;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Short definition of the runtime libraries used during development.
    /// </summary>
    public sealed class RuntimeDefinition
    {
        private const string FieldPath = "NDKRemotePath";

        public RuntimeDefinition(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException("path");

            Folder = folder;
        }

        #region Properties

        public string Folder
        {
            get;
            private set;
        }

        #endregion


        /// <summary>
        /// Retrieves info about used runtime libraries from registry.
        /// </summary>
        public static RuntimeDefinition Load()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings;

            string folder = null;

            try
            {
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
            }
            catch
            {
                // no plugin info found at all...
                return null;
            }
            if (settings == null)
                return null;

            try
            {
                var xFolder = settings.GetValue(FieldPath);
                if (xFolder != null)
                    folder = System.IO.Path.GetFullPath(xFolder.ToString());
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            // verify arguments:
            if (string.IsNullOrEmpty(folder))
                return null;

            return new RuntimeDefinition(folder);
        }

        /// <summary>
        /// Removes runtime libraries info from the registry.
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

                settings.DeleteValue(FieldPath, false);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Saves runtime libraries info into the registry.
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

                settings.SetValue(FieldPath, Folder);
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
