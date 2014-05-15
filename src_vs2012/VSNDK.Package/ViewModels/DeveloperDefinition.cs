using System;
using System.IO;
using Microsoft.Win32;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// Short settings about developer doing the development.
    /// </summary>
    internal sealed class DeveloperDefinition
    {
        private const string DefaultCertificateName = "author.p12";
        private const string FieldCertificateFileName = "certificate";
        private const string FieldCskPassword = "CSKPass";

        public DeveloperDefinition(string certificatePath, string cskPassword)
        {
            if (string.IsNullOrEmpty(certificatePath))
                throw new ArgumentNullException("certificatePath");

            CertificateFileName = certificatePath;
            CskPassword = cskPassword;
        }

        #region Properties

        /// <summary>
        /// Gets the path to the author.p12 file.
        /// </summary>
        public string CertificateFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Password set by developer to the certificate.
        /// </summary>
        public string CskPassword
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Updates stored CSK password to a new value.
        /// </summary>
        public void UpdatePassword(string password, bool remember)
        {
            CskPassword = password;
            if (remember)
                SavePassword();
        }

        /// <summary>
        /// Stores password into registry.
        /// </summary>
        public void SavePassword()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                if (CskPassword == null)
                    CskPassword = string.Empty;

                settings.SetValue(FieldCskPassword, GlobalFunctions.Encrypt(CskPassword));
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Checks, if the CSK password is stored in registry.
        /// </summary>
        public bool CheckIsPasswordSaved()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.OpenSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return false;

                if (CskPassword == null)
                    CskPassword = string.Empty;

                return settings.GetValue(FieldCskPassword, null) != null;
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Removes password stored inside registry.
        /// </summary>
        public static void DeletePassword()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteValue(FieldCskPassword, false);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Creates new developer-definition object based on info read from registry and around.
        /// </summary>
        public static DeveloperDefinition Load(string dataPath)
        {
            if (string.IsNullOrEmpty(dataPath))
                throw new ArgumentNullException("dataPath");

            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings;

            string certificateFileName = null;
            string cskPassword = null;

            try
            {
                settings = registry.OpenSubKey(RunnerDefaults.RegistryPath);
            }
            catch
            {
                // no device definition
                return null;
            }
            if (settings == null)
                return null;

            // CERTIFICATE
            try
            {
                object xCertificate = settings.GetValue(FieldCertificateFileName);
                if (xCertificate != null)
                    certificateFileName = xCertificate.ToString();
            }
            catch
            {
            }
            // set default, if loading failed:
            if (string.IsNullOrEmpty(certificateFileName))
                certificateFileName = Path.Combine(dataPath, DefaultCertificateName);

            // IP
            try
            {
                object xPassword = settings.GetValue(FieldCskPassword);
                if (xPassword != null)
                    cskPassword = GlobalFunctions.Decrypt(xPassword.ToString());
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            return new DeveloperDefinition(certificateFileName, cskPassword);
        }
    }
}
