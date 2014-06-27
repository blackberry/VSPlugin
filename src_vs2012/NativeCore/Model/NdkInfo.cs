using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Descriptor of a locally installed NDK.
    /// </summary>
    public sealed class NdkInfo : ApiInfo
    {
        private const string DescriptorFileName = "blackberry-sdk-descriptor.xml";

        public NdkInfo(string filePath, string name, Version version, string hostPath, string targetPath, DeviceFamilyType type, DeviceInfo[] devices, PermissionInfo[] permissions)
            : base(name, version, type)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");
            if (devices == null)
                throw new ArgumentNullException("devices");
            if (permissions == null)
                throw new ArgumentNullException("");

            FilePath = filePath;
            HostPath = hostPath;
            TargetPath = targetPath;
            Devices = devices;
            Permissions = permissions;
        }

        public NdkInfo(string filePath, string name, Version version, string hostPath, string targetPath, DeviceFamilyType type)
            : base(name, version, type)
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

            // load permissions from the same descriptor file as devices were stored in:
            if (devices != null)
            {
                if (devices.Length == 0)
                {
                    // found descriptor file, but was empty... heuristics says - it's a PlayBook
                    // then even override type, what caller provided
                    //  - there is one case when it can happen - when developer added custom NDK to tablet - and there is no other way to distinguish phone-tablet
                    Permissions = PermissionInfo.CreatePlayBookList();
                    Type = DeviceFamilyType.Tablet;
                }
                else
                {
                    Permissions = LoadPermissions(ndkDescriptor);
                }
            }
            else
            {
                // in case no descriptor file was found, use a default list:
                Permissions = PermissionInfo.CreateDefaultList();
            }

            // OK, give up, and say it's unknown, if still null:
            Devices = devices ?? new DeviceInfo[0];
            Details = ToShortDeviceDescription();
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

        public PermissionInfo[] Permissions
        {
            get;
            private set;
        }

        public override bool IsInstalled
        {
            get
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
        }

        #endregion

        /// <summary>
        /// Compares two paths, if they are identical.
        /// </summary>
        private static bool HasPath(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA) && string.IsNullOrEmpty(pathB))
                return true;

            // assuming that paths we acquired by Path.GetFullPath(...)
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
            result.Append(" - ").AppendLine(Type.ToString());
            
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
#if DEBUG
            result.Append(" - descriptor: ").AppendLine(FilePath);
#endif

            result.AppendLine();
            result.AppendLine("Permissions:");
            if (Permissions.Length > 0)
            {
                foreach (var permission in Permissions)
                {
                    result.Append(" - ").Append(permission.Name).Append(" (").Append(permission.ID).AppendLine(")");
                }
            }
            else
            {
                result.AppendLine(" No available application permissions found.");
            }

            return result.ToString();
        }

        private string ToShortDeviceDescription()
        {
            if (Devices == null || Devices.Length == 0)
                return "Device support unknown";

            var result = new StringBuilder("Supports ");
            int i = 0;

            foreach (var device in Devices)
            {
                if (i > 0)
                    result.Append(", ");
                result.Append(device.ModelFullName);
                i++;
            }

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
            DeviceFamilyType type = DeviceFamilyType.Phone;
            bool allowTypeOverride = true;

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
                        case "edition":
                            type = DeviceFamilyHelper.GetTypeFromString(reader.ReadString());
                            allowTypeOverride = false;
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
                            if (allowTypeOverride)
                            {
                                type = DeviceFamilyType.Tablet;
                            }

                            // try first to load info about installation:
                            var installInfoPath = Path.Combine(targetPath, "..", "..", "install", "info.txt");
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

                            // if that failed, maybe there is a version inside the folder?
                            if (version == null)
                            {
                                version = GetVersionFromFolderName(name);
                            }

                            // ok, give up, assign anything:
                            if (version == null)
                            {
                                version = CreateTabletInfo().Version;
                            }
                        }

                        // try to define info about the installation:
                        return new NdkInfo(fileName, name, version, hostPath, targetPath, type);
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
                TraceLog.WriteException(ex, "Unable to load device list from NDK descriptor file: \"{0}\"", ndkDescriptorFileName);
                return null;
            }
        }

        private static PermissionInfo[] LoadPermissions(string ndkDescriptorFileName)
        {
            try
            {
                if (!File.Exists(ndkDescriptorFileName))
                    return null;

                // try to load info about application permissions from the NDK:
                using (var fileReader = new StreamReader(ndkDescriptorFileName, Encoding.UTF8))
                {
                    using (var descReader = XmlReader.Create(fileReader))
                    {
                        return PermissionInfo.Load(descReader);
                    }
                }

            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load permissions from NDK descriptor file: \"{0}\"", ndkDescriptorFileName);
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
                                        if (info != null && info.IsInstalled)
                                        {
                                            var existingIndex = IndexOf(result, info);

                                            // and store only unique ones:
                                            // (if NDK info exist with exactly the same paths, prefer to keep the one with longer name)
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
            if (info != null && list != null)
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
                var type = DeviceFamilyType.Phone;

                if (version == null)
                {
                    // is there an installation folder with version (used by PlayBook NDKs)?
                    var installInfoPath = Path.Combine(folder, "..", "install", "info.txt");
                    if (File.Exists(installInfoPath))
                    {
                        try
                        {
                            version = GetVersionFromInstallationInfo(File.ReadAllLines(installInfoPath));
                            type = DeviceFamilyType.Tablet;
                        }
                        catch (Exception ex)
                        {
                            type = DeviceFamilyType.Unknown;
                            TraceLog.WriteException(ex, "Unable to read installation info from file: \"{0}\"", installInfoPath);
                        }
                    }

                    if (version == null)
                        version = new Version(10, 0, 1);
                }

                var prefix = type == DeviceFamilyType.Tablet ? "BlackBerry Native SDK for Tablet OS " : "BlackBerry Local SDK ";
                return new NdkInfo(null, string.Concat(prefix, version, " /", DateTime.Now.ToString("yyyy-MM-dd"), "/"),
                                   version, Path.Combine(hostDirs[0], "win32", "x86"), Path.Combine(folder, "qnx6"), type);
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
    <edition>{4}</edition>
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
                inst["edition"].InnerText = DeviceFamilyHelper.GetTypeToString(Type); // VS-Plugin extension
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

        /// <summary>
        /// Creates the shimmed definition instance.
        /// </summary>
        public NdkDefinition ToDefinition()
        {
            return new NdkDefinition(HostPath, TargetPath, Type);
        }
    }
}
