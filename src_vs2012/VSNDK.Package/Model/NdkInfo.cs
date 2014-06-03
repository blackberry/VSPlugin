using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using RIM.VSNDK_Package.Diagnostics;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Descriptor of a locally installed NDK.
    /// </summary>
    internal sealed class NdkInfo : ApiInfo, IComparable<NdkInfo>
    {
        private const string DescriptorFileName = "blackberry-sdk-descriptor.xml";

        public NdkInfo(string filePath, string name, Version version, string hostPath, string targetPath, DeviceInfo[] devices)
            : base(name, version)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");
            if (devices == null)
                throw new ArgumentNullException("devices");

            FilePath = filePath;
            HostPath = hostPath;
            TargetPath = targetPath;
            Devices = devices;
        }

        public NdkInfo(string filePath, string name, Version version, string hostPath, string targetPath)
            : base(name, version)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");

            FilePath = filePath;
            HostPath = hostPath;
            TargetPath = targetPath;

            ////////////////////////
            // and now load devices, based on some heuristics, keeping in mind, that this field can't be never null:
            var ndkDescriptor = Path.Combine(targetPath, DescriptorFileName);
            DeviceInfo[] devices = LoadDevices(ndkDescriptor);

            // if failed to load devices, try to find one folder above:
            if (devices == null)
            {
                ndkDescriptor = Path.Combine(targetPath, "..", DescriptorFileName);
                devices = LoadDevices(ndkDescriptor);
            }

            // or another above:
            if (devices == null)
            {
                ndkDescriptor = Path.Combine(targetPath, "..", "..", DescriptorFileName);
                devices = LoadDevices(ndkDescriptor);
            }

            // OK, give up, and say it's unknown:
            Devices = devices ?? new DeviceInfo[0];
        }

        #region Properties

        public string FilePath
        {
            get;
            private set;
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

        public DeviceInfo[] Devices
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Checks if given NDK is really available.
        /// </summary>
        public bool Exists()
        {
            try
            {
                return Directory.Exists(HostPath) && Directory.Exists(TargetPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two paths, if they are identical.
        /// </summary>
        private static bool HasPath(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA) && string.IsNullOrEmpty(pathB))
                return true;

            // assuming that paths we aquired by Path.GetFullPath(...)
            return string.Compare(pathA, pathB, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        /// <summary>
        /// Checks if specified path points to the same folder as TargetPath.
        /// </summary>
        public bool HasTargetPath(string path)
        {
            return HasPath(TargetPath, path);
        }

        /// <summary>
        /// Checks if given NDK info has identical paths.
        /// </summary>
        public bool Matches(string hostPath, string targetPath)
        {
            return HasPath(HostPath, hostPath) && HasPath(TargetPath, targetPath);
        }

        /// <summary>
        /// Checks if given NDKs are identical.
        /// </summary>
        public bool Matches(NdkInfo ndk)
        {
            return HasPath(HostPath, ndk != null ? ndk.HostPath : null) && HasPath(TargetPath, ndk != null ? ndk.TargetPath : null);
        }

        /// <summary>
        /// Gets the long description for this NDK.
        /// </summary>
        public string ToLongDescription()
        {
            var result = new StringBuilder();

            result.AppendLine(Name).AppendLine();
            result.AppendLine("Version:");
            result.Append(" - ").AppendLine(Version.ToString());
            result.AppendLine();

            result.AppendLine("Devices:");
            if (Devices.Length > 0)
            {
                foreach (var device in Devices)
                {
                    result.Append(" - ").Append(device.ModelFullName).Append(", ").AppendLine(device.ModelFamily);
                    result.Append("     ")
                          .Append(device.ScreenDPI)
                          .Append("ppi, icon: ")
                          .Append(((int) device.IconResolution.Width).ToString())
                          .Append("x")
                          .AppendLine(((int) device.IconResolution.Height).ToString());
                }
            }
            else
            {
                result.AppendLine(" No supported device information found.");
            }
            result.AppendLine();
            result.AppendLine("Paths:");
            result.Append(" - host: ").AppendLine(HostPath);
            result.Append(" - target: ").AppendLine(TargetPath);

            return result.ToString();
        }

        /// <summary>
        /// Loads info about a single NDK from specific config file, given as XML.
        /// </summary>
        public static NdkInfo Load(string fileName, XmlReader reader)
        {
            if (reader == null)
                return null;

            string name = null;
            Version version = null;
            string hostPath = null;
            string targetPath = null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "name":
                            name = reader.ReadString();
                            break;
                        case "version":
                            version = new Version(reader.ReadString());
                            break;
                        case "host":
                            hostPath = Path.GetFullPath(reader.ReadString());
                            break;
                        case "target":
                            targetPath = Path.GetFullPath(reader.ReadString());
                            break;
                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "installation")
                {
                    if (!string.IsNullOrEmpty(targetPath) && !string.IsNullOrEmpty(hostPath))
                    {
                        // is it a PlayBook NDK, which has no 'version' field?
                        if (version == null && !string.IsNullOrEmpty(name))
                        {
                            version = GetVersionFromFolderName(name);
                        }

                        // try to define info about the installation:
                        return new NdkInfo(fileName, name, version, hostPath, targetPath);
                    }

                    return null;
                }
            }

            return null;
        }

        private static DeviceInfo[] LoadDevices(string ndkDescriptorFileName)
        {
            try
            {
                if (!File.Exists(ndkDescriptorFileName))
                    return null;

                // try to load info about supported devices by the NDK:
                using (var fileReader = new StreamReader(ndkDescriptorFileName, Encoding.UTF8))
                {
                    using (var descReader = XmlReader.Create(fileReader))
                    {
                        return DeviceInfo.Load(descReader);
                    }
                }
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load NDK descriptor file: \"{0}\"", ndkDescriptorFileName);

                return null;
            }
        }

        /// <summary>
        /// Loads info about all installed NDKs.
        /// </summary>
        public static NdkInfo[] Load(params string[] globalNdkConfigFolders)
        {
            if (globalNdkConfigFolders == null)
                throw new ArgumentNullException("globalNdkConfigFolders");

            var result = new List<NdkInfo>();

            // list all configuration files, across all known folders:
            foreach (var folder in globalNdkConfigFolders)
            {
                if (Directory.Exists(folder))
                {
                    try
                    {
                        var files = Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories);

                        foreach (var file in files)
                        {
                            try
                            {
                                // read them:
                                using (var fileReader = new StreamReader(file, Encoding.UTF8))
                                {
                                    using (var reader = XmlReader.Create(fileReader))
                                    {
                                        var info = Load(file, reader);
                                        if (info != null && info.Exists())
                                        {
                                            var existingIndex = IndexOf(result, info);

                                            // and store only unique ones:
                                            // (if NDK info exist with exacly the same paths, prefer to keep the one with longer name)
                                            if (existingIndex >= 0)
                                            {
                                                var existingItem = result[existingIndex];
                                                if (existingItem.Name != null && info.Name != null && existingItem.Name.Length < info.Name.Length)
                                                {
                                                    result.RemoveAt(existingIndex);
                                                    result.Add(info);
                                                }
                                            }
                                            else
                                            {
                                                result.Add(info);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                TraceLog.WriteException(ex, "Unable to open configuration file: \"{0}\"", file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to load info about configuration files from folder: \"{0}\"", folder);
                    }
                }
            }

            result.Sort();
            return result.ToArray();
        }

        /// <summary>
        /// Returns an index of a given NdkInfo inside a collection.
        /// The search is based by matching few critical properties of an object.
        /// </summary>
        public static int IndexOf(IEnumerable<NdkInfo> list, NdkInfo info)
        {
            if (info != null)
            {
                int i = 0;
                foreach (var item in list)
                {
                    if (item.Matches(info))
                        return i;
                    i++;
                }
            }
            return -1;
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(NdkInfo other)
        {
            if (other == null)
                return 1;

            int cmp = Version.CompareTo(other.Version);
            if (cmp != 0)
                return cmp;

            return string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Try to find an NDK definition somewhere in specified folder or around.
        /// </summary>
        public static NdkInfo Scan(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return null;

            if (!Directory.Exists(folder))
                return null;

            // is it a target folder directly?
            NdkInfo info = ScanByTargetFolder(folder);
            if (info != null)
                return info;

            // is it a root folder?
            var targetDirs = Directory.GetDirectories(folder, "target*");
            if (targetDirs.Length != 0)
            {
                // get the first folder with a descriptor:
                foreach (var target in targetDirs)
                {
                    info = ScanByTargetFolder(target);
                    if (info != null)
                        return info;
                }
            }

            // is it a host folder?
            var parentDir = Path.GetDirectoryName(folder);
            if (string.IsNullOrEmpty(parentDir))
                return null;

            targetDirs = Directory.GetDirectories(parentDir, "target*");
            if (targetDirs.Length == 0)
                return null;

            // get first folder with a descriptor:
            foreach (var target in targetDirs)
            {
                info = ScanByTargetFolder(targetDirs[0]);
                if (info != null)
                    return info;
            }

            return null;
        }

        private static NdkInfo ScanByTargetFolder(string folder)
        {
            var parentDir = Path.GetDirectoryName(folder);
            if (string.IsNullOrEmpty(parentDir))
                return null;

            var devicesDescriptorFileName = Path.Combine(folder, DescriptorFileName);
            if (File.Exists(devicesDescriptorFileName))
            {
                // find host folder
                var hostDirs = Directory.GetDirectories(parentDir, "host*");
                if (hostDirs.Length == 0)
                    return null;

                // extract version out of the current folder name
                var version = GetVersionFromFolderName(Path.GetFileName(folder));
                if (version == null)
                {
                    // is there an installation folder with version (used by PlayBook NDKs)?
                    var installInfoPath = Path.Combine(folder, "..", "install", "info.txt");
                    if (File.Exists(installInfoPath))
                    {
                        try
                        {
                            version = GetVersionFromInstallationInfo(File.ReadAllLines(installInfoPath));
                        }
                        catch (Exception ex)
                        {
                            TraceLog.WriteException(ex, "Unable to read installation info from file: \"{0}\"", installInfoPath);
                        }
                    }

                    if (version == null)
                        version = new Version(10, 0, 1);
                }

                return new NdkInfo(null, string.Concat("BlackBerry Local SDK ", version, " /", DateTime.Now.ToString("yyyy-MM-dd"), "/"),
                                   version, Path.Combine(hostDirs[0], "win32", "x86"), Path.Combine(folder, "qnx6"));
            }

            return null;
        }

        private static Version GetVersionFromInstallationInfo(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return null;

            // find the line, that describes the target version:
            foreach (var line in lines)
            {
                if (line.StartsWith("target", StringComparison.OrdinalIgnoreCase))
                {
                    var index = line.IndexOf('=');
                    string versionString;
                    if (index < 0)
                        versionString = line.Substring(7).Trim(); // one char after 'target' assumed to be separator
                    else
                        versionString = line.Substring(index + 1).Trim();

                    return string.IsNullOrEmpty(versionString) ? null : new Version(versionString);
                }
            }

            return null;
        }

        private static Version GetVersionFromFolderName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return null;

            int i = directoryName.Length;

            // find the version substring at the end of the name:
            while (i > 0 && directoryName[i - 1] == '_' || directoryName[i - 1] == '.' || char.IsDigit(directoryName[i - 1]))
                i--;

            // we might read one char too much:
            if (i < directoryName.Length && !char.IsDigit(directoryName[i]))
                i++;

            // parse:
            var versionString = directoryName.Substring(i).Trim().Replace('_', '.');
            if (string.IsNullOrEmpty(versionString) || versionString.IndexOf('.') < 0)
                return null;

            return new Version(versionString);
        }

        public bool Save(string outputDirectory)
        {
            if (string.IsNullOrEmpty(outputDirectory))
                throw new ArgumentNullException("outputDirectory");

            // make sure, the output directory exists:
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to create directory for NDK info cache");
                return false;
            }

            // normalize file name:
            var fileName = new StringBuilder(string.IsNullOrEmpty(Name) ? "vsplugin_target_" + Version + DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + ".xml" : Name + ".xml");

            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];
                if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
                {
                    fileName[i] = '_';
                }
            }

            // generate content:
            const string content = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<qnxSystemDefinition>
  <installation>
    <name>{0}</name>
    <version>{1}</version>
    <host>{2}</host>
    <target>{3}</target>
    <annotation>
      <appInfo source=""Custom SDK""/>
    </annotation>
  </installation>
</qnxSystemDefinition>
";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);

            string name = Name ?? string.Empty;
            string version = Version.ToString();
            string hostPath = HostPath.Replace('\\', '/');
            string targetPath = TargetPath.Replace('\\', '/');

            var inst = doc.DocumentElement["installation"];
            if (inst != null)
            {
                inst["name"].InnerText = name;
                inst["version"].InnerText = version;
                inst["host"].InnerText = hostPath;
                inst["target"].InnerText = targetPath;
            }
            else
            {
                throw new FormatException("Invalid data to process");
            }

            // store it:
            var fullFilePath = Path.Combine(outputDirectory, fileName.ToString());
            doc.Save(fullFilePath);
            FilePath = fullFilePath;
            return true;
        }
    }
}
