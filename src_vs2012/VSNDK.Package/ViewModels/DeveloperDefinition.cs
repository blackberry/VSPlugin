using System;
using System.IO;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// Short settings about developer doing the development.
    /// </summary>
    internal sealed class DeveloperDefinition
    {
        /// <summary>
        /// Default name of the certificate file.
        /// </summary>
        internal const string DefaultCertificateName = "author.p12";
        private const string DefaultCskName = "bbidtoken.csk";
        private const string FieldCertificateFileName = "certificate";
        private const string FieldCskPassword = "CSKPass";

        public DeveloperDefinition(string dataPath, string certificatePath, string cskPassword)
        {
            if (string.IsNullOrEmpty(dataPath))
                throw new ArgumentNullException("dataPath");

            DataPath = dataPath;
            CertificateFileName = certificatePath;
            CskPassword = cskPassword;

            // get the author directly from certificate:
            Name = LoadIssuer(CertificateFullPath, CskPassword);
        }

        #region Properties

        /// <summary>
        /// Gets the name of the developer (publisher).
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the indication, if a name was loaded for this developer (publisher) from a certificate.
        /// </summary>
        public bool HasName
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        /// <summary>
        /// Gets the location, where all developer configuration files are stored.
        /// </summary>
        public string DataPath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the name of certificate file (author.p12).
        /// </summary>
        public string CertificateFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path to the developers certificate.
        /// </summary>
        public string CertificateFullPath
        {
            get { return string.IsNullOrEmpty(CertificateFileName) ? null : Path.Combine(DataPath, CertificateFileName); }
        }

        /// <summary>
        /// Gets full path to the BlackBerry ID token file (bbidtoken.csk).
        /// </summary>
        public string BlackBerryTokenFullPath
        {
            get { return Path.Combine(DataPath, DefaultCskName); }
        }

        /// <summary>
        /// Password set by developer to the certificate.
        /// </summary>
        public string CskPassword
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks, if CskPassword is setup.
        /// </summary>
        public bool HasPassword
        {
            get { return !string.IsNullOrEmpty(CskPassword); }
        }

        /// <summary>
        /// Checks if developer completed registration.
        /// </summary>
        public bool IsRegistered
        {
            get { return !string.IsNullOrEmpty(CertificateFileName) && File.Exists(CertificateFullPath); }
        }

        /// <summary>
        /// Checks if developer started registration and downloaded token file.
        /// </summary>
        public bool HasBlackBerryTokenFile
        {
            get { return File.Exists(BlackBerryTokenFullPath); }
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
        /// Tries to reload issuer from the certificate.
        /// </summary>
        public string UpdateName(string password)
        {
            return Name = LoadIssuer(CertificateFullPath, string.IsNullOrEmpty(password) ? CskPassword : password);
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
        public bool IsPasswordSaved
        {
            get
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
        }

        /// <summary>
        /// Removes password stored inside registry and cached in this class.
        /// </summary>
        public void ClearPassword()
        {
            DeletePassword();
            CskPassword = null;
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
        /// Updates path to the certificate inside the registry.
        /// </summary>
        private void SaveCertificatePath()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                if (string.Compare(DefaultCertificateName, CertificateFileName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    settings.DeleteValue(FieldCertificateFileName, false);
                else
                    settings.SetValue(FieldCertificateFileName, CertificateFileName);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Removes certificate path from registry.
        /// </summary>
        private void DeleteCertificatePath()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteValue(FieldCertificateFileName, false);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Updates the certificate file name and save it into the registry.
        /// </summary>
        public void UpdateCertificate(string fileName)
        {
            var folder = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(folder))
                throw new ArgumentOutOfRangeException("fileName", "Invalid name, folder is not expected inside, only file name");
            if (File.Exists(Path.Combine(DataPath, folder)))
                throw new ArgumentOutOfRangeException("fileName", "File doesn't existing in designated certificate storage");

            ClearPassword();
            CertificateFileName = fileName;
            Name = null;

            if (string.Compare(fileName, DefaultCertificateName, StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                DeleteCertificatePath();
            }
            else
            {
                SaveCertificatePath();
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
                certificateFileName = DefaultCertificateName;

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

            return new DeveloperDefinition(dataPath, certificateFileName, cskPassword);
        }

        /// <summary>
        /// Loads 'issuer' field from the X.509 certificate.
        /// </summary>
        public static string LoadIssuer(string certificateFileName, string password)
        {
            try
            {
                if (!File.Exists(certificateFileName) || string.IsNullOrEmpty(password))
                    return null;

                /* PH: OK - this did work, but not for WinXP, as this one doesn't support SHA512withECDSA algorithm...
                var cert = new X509Certificate(certificateFileName, password);
                var issuer = cert.Issuer;
                 */
                string issuer;
                using (var runner = new KeyToolInfoRunner(RunnerDefaults.ToolsDirectory, certificateFileName, password))
                {
                    // invoke
                    if (!runner.Execute())
                        return null;

                    // grab results:
                    issuer = runner.Issuer;
                }

                if (issuer != null && issuer.StartsWith("CN=", StringComparison.InvariantCultureIgnoreCase))
                    issuer = issuer.Substring(3).Trim();
                if (issuer != null && issuer.StartsWith("CommonName=", StringComparison.InvariantCultureIgnoreCase))
                    issuer = issuer.Substring(11).Trim();

                return issuer;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to read data from certificate: \"{0}\"", certificateFileName);
                return null;
            }
        }

        /// <summary>
        /// Gets the list of files, that are assumed to be a developer profile for signing applications.
        /// </summary>
        private string[] GetProfileFiles()
        {
            return new[] { CertificateFileName, "bbidtoken.csk", "barsigner.db",
                                    "bbsigner.csk", "bb_id_rsa", "bb_id_rsa.pub",

                                    // PH: TODO: but I have also files: for tablets?
                                    "barsigner.csk", "bbt_id_rsa", "bbt_id_rsa.pub"};
        }

        /// <summary>
        /// Saves all developer profile info into specified file.
        /// </summary>
        public bool BackupProfile(string outputFile)
        {
            if (string.IsNullOrEmpty(outputFile))
                return false;

            var fileNames = GetProfileFiles();

            try
            {
                using (var package = Package.Open(outputFile, FileMode.Create))
                {
                    foreach (var name in fileNames)
                    {
                        Append(package, name, Path.Combine(DataPath, name));
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to export profile to file: \"{0}\"", outputFile);
                return false;
            }
        }

        /// <summary>
        /// Appends file to the ZIP package.
        /// </summary>
        private static void Append(Package package, string name, string fullPathName)
        {
            if (File.Exists(fullPathName))
            {
                Uri uri = PackUriHelper.CreatePartUri(new Uri(name, UriKind.Relative));
                PackagePart part = package.CreatePart(uri, string.Empty);

                using (var fileStream = new FileStream(fullPathName, FileMode.Open, FileAccess.Read))
                {
                    CopyStream(fileStream, part.GetStream());
                }
            }
        }

        /// <summary>
        /// Copy a stream from one stream to another.
        /// </summary>
        private static void CopyStream(Stream source, Stream target)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (target == null)
                throw new ArgumentNullException("target");

            var buffer = new byte[0x1000];
            int bytesRead;

            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                target.Write(buffer, 0, bytesRead);
            }
        }

        /// <summary>
        /// Extracts the developer profile.
        /// </summary>
        public bool RestoreProfile(string inputFile)
        {
            if (string.IsNullOrEmpty(inputFile))
                return false;

            if (File.Exists(inputFile))
            {
                string p12FileName = null;

                try
                {
                    // make the password invalid:
                    DeletePassword();

                    // extract all files that are within the backup package:
                    using (var package = Package.Open(inputFile, FileMode.Open, FileAccess.ReadWrite))
                    {
                        foreach (var part in package.GetParts())
                        {
                            var name = ExtractFile(part, DataPath);
                            if (name != null && name.EndsWith(".p12"))
                            {
                                p12FileName = Path.GetFileName(name);
                            }
                        }
                    }

                    // no certificate found inside?
                    if (string.IsNullOrEmpty(p12FileName))
                        return false;

                    // update save it for future reference:
                    CertificateFileName = p12FileName;
                    SaveCertificatePath();
                    return true;
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Impossible to restore profile: \"{0}\"", inputFile);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Extract file under specified directory.
        /// </summary>
        public static string ExtractFile(PackagePart part, string folder)
        {
            // initially create file under the folder specified
            string filePath = part.Uri.OriginalString.Replace('/', Path.DirectorySeparatorChar);

            // remove trailing directory separator:
            if (!string.IsNullOrEmpty(filePath) && filePath[0] == Path.DirectorySeparatorChar)
            {
                filePath = filePath.TrimStart(Path.DirectorySeparatorChar);
            }

            filePath = Path.Combine(folder, filePath);
            var dirName = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            // extract file:
            using (var output = File.Create(filePath))
            {
                CopyStream(part.GetStream(), output);
            }

            return filePath;
        }

        /// <summary>
        /// Writes a BlackBerry ID token file.
        /// </summary>
        public void SaveBlackBerryToken(string content)
        {
            var fileName = BlackBerryTokenFullPath;

            if (File.Exists(fileName))
                File.Delete(fileName);

            if (!string.IsNullOrEmpty(content))
            {
                File.WriteAllText(fileName, content);
            }
        }

        /// <summary>
        /// Delete all files that belong to the developer profile.
        /// Removes its password etc.
        /// </summary>
        public void DeleteProfile()
        {
            var files = GetProfileFiles();

            foreach (var fileName in files)
            {
                var fullName = Path.Combine(DataPath, fileName);
                if (File.Exists(fullName))
                    File.Delete(fullName);
            }

            DeleteCertificatePath();
            ClearPassword();
            CertificateFileName = null;
            Name = null;
        }

        /// <summary>
        /// Removes some profile temporary files, that are not needed anymore, when registration process completed.
        /// </summary>
        public void CleanupProfile()
        {
            if (!IsRegistered)
            {
                DeleteProfile();
            }
        }
    }
}
