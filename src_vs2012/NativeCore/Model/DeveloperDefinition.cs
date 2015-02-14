using System;
using System.IO;
using System.IO.Packaging;
using System.Text;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.Tools;
using Microsoft.Win32;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Short settings about developer doing the development.
    /// </summary>
    public sealed class DeveloperDefinition
    {
        /// <summary>
        /// Default name of the certificate file.
        /// </summary>
        public const string DefaultCertificateName = "author.p12";
        private const string DefaultCskTokenName = "bbidtoken.csk";
        private const string DefaultTabletSignerName = "barsigner.db";
        private const string DefaultTabletCskTokenName = "barsigner.csk";
        private const string FieldCertificateFileName = "certificate";

        private const string FieldCskPassword = "CSKPass";
        private const string FieldCachedAuthorID = "CachedAuthorID";
        private const string FieldCachedAuthorName = "CachedAuthorName";

        private string _name;
        private AuthorInfo _cachedAuthor;
        private CskTokenInfo _cskToken;
        private CskTokenInfo _cskTabletToken;

        public DeveloperDefinition(string dataPath, string certificatePath, string cskPassword, AuthorInfo authorInfo)
        {
            if (string.IsNullOrEmpty(dataPath))
                throw new ArgumentNullException("dataPath");

            DataPath = dataPath;
            CertificateFileName = certificatePath ?? DefaultCertificateName;
            CskPassword = cskPassword;
            _cachedAuthor = authorInfo;
        }

        #region Properties

        /// <summary>
        /// Gets the name of the developer (publisher).
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name) && HasPassword)
                {
                    // get the author from certificate:
                    _name = LoadIssuer(CertificateFullPath, CskPassword);
                }

                return _name;
            }
            private set { _name = value; }
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
        /// Gets an indication, if the certificate file exists.
        /// </summary>
        public bool HasCertificate
        {
            get { return !string.IsNullOrEmpty(CertificateFileName) && File.Exists(CertificateFullPath); }
        }

        /// <summary>
        /// Gets full path to the BlackBerry ID token file (bbidtoken.csk).
        /// </summary>
        public string CskTokenFullPath
        {
            get { return Path.Combine(DataPath, DefaultCskTokenName); }
        }

        /// <summary>
        /// Gets full path do the BlackBerry Tablet Signer Authority file (barsigner.db).
        /// </summary>
        public string TabletSignerFullPath
        {
            get { return Path.Combine(DataPath, DefaultTabletSignerName); }
        }

        /// <summary>
        /// Gets full path to the BlackBerry Tablet CSK token file (barsigner.csk).
        /// </summary>
        public string TabletCskTokenFullPath
        {
            get { return Path.Combine(DataPath, DefaultTabletCskTokenName); }
        }

        /// <summary>
        /// Gets the BlackBerry ID token information.
        /// </summary>
        public CskTokenInfo Token
        {
            get
            {
                if (_cskToken == null)
                    _cskToken = CskTokenInfo.Load(CskTokenFullPath);

                return _cskToken;
            }
            private set { _cskToken = value; }
        }

        /// <summary>
        /// Gets the BlackBerry Tablet token information.
        /// </summary>
        public CskTokenInfo TabletToken
        {
            get
            {
                if (_cskTabletToken == null)
                    _cskTabletToken = CskTokenInfo.Load(TabletCskTokenFullPath);

                return _cskTabletToken;
            }
            private set { _cskTabletToken = value; }
        }

        /// <summary>
        /// Gets an indication, if BlackBerry ID token exists.
        /// </summary>
        public bool HasToken
        {
            get { return File.Exists(CskTokenFullPath) && Token != null; }
        }

        /// <summary>
        /// Gets an indication, if BlackBerry Tablet token exists.
        /// </summary>
        public bool HasTabletToken
        {
            get { return File.Exists(TabletCskTokenFullPath) && TabletToken != null; }
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
        /// Checks if developer completed any kind of registration.
        /// </summary>
        public bool IsRegistered
        {
            get { return IsBB10Registered || IsTabletRegistered; }
        }

        /// <summary>
        /// Checks if developer completed BB10 registration.
        /// </summary>
        public bool IsBB10Registered
        {
            get { return HasCertificate && File.Exists(CskTokenFullPath); }
        }

        /// <summary>
        /// Checks if developer completed tablet registration.
        /// </summary>
        public bool IsTabletRegistered
        {
            get
            {
                return HasCertificate
                       && File.Exists(TabletCskTokenFullPath)
                       && File.Exists(TabletSignerFullPath) && GetFileSize(TabletSignerFullPath) > 0;
            }
        }

        /// <summary>
        /// Gets the cached info about author (publisher).
        /// This is data put directly inside bar-descriptor.xml files and usually obtained, when creating debug-tokens.
        /// </summary>
        public AuthorInfo CachedAuthor
        {
            get { return _cachedAuthor; }
            set
            {
                if (value == null)
                {
                    DeleteCachedAuthorInfo();
                    _cachedAuthor = null;
                }
                else
                {
                    // is there anything new?
                    if (_cachedAuthor != null && string.CompareOrdinal(value.ID, _cachedAuthor.ID) == 0 && string.CompareOrdinal(value.Name, _cachedAuthor.Name) == 0)
                    {
                        return;
                    }

                    _cachedAuthor = value;
                    SaveCachedAuthorInfo(value);
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        private static long GetFileSize(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Length;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Invalidates loaded CSK tokens to force them load from disk again.
        /// </summary>
        public void InvalidateTokens()
        {
            Token = null;
            TabletToken = null;
        }

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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
                if (settings == null)
                    return;

                if (CskPassword == null)
                    CskPassword = string.Empty;

                settings.SetValue(FieldCskPassword, GlobalHelper.Encrypt(CskPassword));
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
                    settings = registry.OpenSubKey(ConfigDefaults.RegistryPath);
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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
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
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
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
            if (!File.Exists(Path.Combine(DataPath, fileName)))
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
        /// Updates the certificate file name and password and saves it into registry if required.
        /// </summary>
        public void UpdateCertificate(string fileName, string password, bool remember)
        {
            UpdateCertificate(fileName);
            UpdatePassword(password, remember);
            UpdateName(null);
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
            string authorID = null;
            string authorName = null;
            AuthorInfo authorInfo = null;

            try
            {
                settings = registry.OpenSubKey(ConfigDefaults.RegistryPath);
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

            // PASSWORD
            try
            {
                object xPassword = settings.GetValue(FieldCskPassword);
                if (xPassword != null)
                    cskPassword = GlobalHelper.Decrypt(xPassword.ToString());
            }
            catch
            {
            }

            // CACHED AUTHOR ID + NAME
            try
            {
                object xAuthorID = settings.GetValue(FieldCachedAuthorID);
                if (xAuthorID != null)
                    authorID = xAuthorID.ToString();

                object xAuthorName = settings.GetValue(FieldCachedAuthorName);
                if (xAuthorName != null)
                    authorName = xAuthorName.ToString();
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            // having author info with any value set as non-'null' is OK, as it comes from caches...
            if (!string.IsNullOrEmpty(authorID) || !string.IsNullOrEmpty(authorName))
            {
                authorInfo = new AuthorInfo(authorID, authorName);
            }

            return new DeveloperDefinition(dataPath, certificateFileName, cskPassword, authorInfo);
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
                using (var runner = new KeyToolInfoRunner(certificateFileName, password))
                {
                    // invoke
                    if (!runner.Execute())
                        return null;
                    if (runner.Info == null)
                        return null;

                    // grab results:
                    issuer = runner.Info.Issuer;
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
        /// Removes info about cached author (publisher) from registry.
        /// </summary>
        private static void DeleteCachedAuthorInfo()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteValue(FieldCachedAuthorID, false);
                settings.DeleteValue(FieldCachedAuthorName, false);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Stores info about specified author (publisher) inside registry.
        /// </summary>
        private static void SaveCachedAuthorInfo(AuthorInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(ConfigDefaults.RegistryPath);
                if (settings == null)
                    return;

                // id
                if (string.IsNullOrEmpty(info.ID))
                    settings.DeleteValue(FieldCachedAuthorID, false);
                else
                    settings.SetValue(FieldCachedAuthorID, info.ID);

                // name
                if (string.IsNullOrEmpty(info.Name))
                    settings.DeleteValue(FieldCachedAuthorName, false);
                else
                    settings.SetValue(FieldCachedAuthorName, info.Name);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Gets the list of files, that are assumed to be a developer profile for signing applications.
        /// </summary>
        private string[] GetProfileFiles()
        {
            return new[] { CertificateFileName, DefaultCskTokenName, "bbsigner.csk",
                                    "bb_id_rsa", "bb_id_rsa.pub",

                                    // and files for PlayBook tablet:
                                    DefaultTabletCskTokenName, DefaultTabletSignerName,
                                    "bbt_id_rsa", "bbt_id_rsa.pub"};
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

                if (part != null)
                {
                    using (var fileStream = new FileStream(fullPathName, FileMode.Open, FileAccess.Read))
                    {
                        CopyStream(fileStream, part.GetStream());
                    }
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
                    Token = null;

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
        public void SaveCskToken(CskTokenInfo token)
        {
            // update stored token in memory:
            Token = token;

            // and on disk:
            var fileName = CskTokenFullPath;

            if (File.Exists(fileName))
                File.Delete(fileName);

            if (token.HasContent)
            {
                File.WriteAllText(fileName, token.Content);
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
            CertificateFileName = DefaultCertificateName;
            Name = null;
            Token = null;
        }

        /// <summary>
        /// Removes some profile temporary files, that are not needed anymore, when registration process completed.
        /// </summary>
        public void CleanupProfile()
        {
            // if there is no certificate file, means all generators failed,
            // so remove all other remaining files...
            if (!File.Exists(CertificateFullPath))
            {
                DeleteProfile();
            }
        }

        /// <summary>
        /// Gets long description in human readable format.
        /// </summary>
        public string ToLongStatusDescription()
        {
             var result = new StringBuilder();

            result.AppendLine("Status:");
            result.Append(" * BlackBerry 10 - ").AppendLine(IsBB10Registered ? "Registered" : (IsTabletRegistered ? "using tablet certificates" : "Unregistered"));
            if (HasToken)
            {
                if (Token.IsValid)
                    result.Append("  > Token expires on ").AppendLine(Token.ValidDateString);
                else
                    result.Append("  > Token expired ").Append(Token.ExpirationDays).AppendLine(" days ago");
            }
            result.Append(" * PlayBook - ").AppendLine(IsTabletRegistered ? "Registered" : "Unregistered");

            if (IsPasswordSaved)
                result.AppendLine(" * password is stored");

            return result.ToString();
        }

        /// <summary>
        /// Gets short description in human readable format.
        /// </summary>
        public string ToShortStatusDescription()
        {
            var result = new StringBuilder();

            result.Append("BB10: ").AppendLine(IsBB10Registered ? "Registered" : (IsTabletRegistered ? "using tablet certificates" : "Unregistered"));
            if (HasToken)
            {
                if (Token.IsValid)
                    result.Append(" (Token expires on ").Append(Token.ValidDateString).AppendLine(")");
                else
                    result.Append(" (Token expired ").Append(Token.ExpirationDays).AppendLine(" days ago)");
            }
            result.Append("PlayBook: ").Append(IsTabletRegistered ? "Registered" : "Unregistered");

            return result.ToString();
        }
    }
}
