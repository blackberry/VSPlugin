using System;
using System.Collections.Generic;
using System.Globalization;
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
        public NdkInfo(string name, Version version, string hostPath, string targetPath, DeviceInfo[] devices)
            : base(name, version)
        {
            if (string.IsNullOrEmpty(hostPath))
                throw new ArgumentNullException("hostPath");
            if (string.IsNullOrEmpty(targetPath))
                throw new ArgumentNullException("targetPath");
            if (devices == null)
                throw new ArgumentNullException("devices");

            HostPath = hostPath;
            TargetPath = targetPath;
            Devices = devices;
        }

        #region Properties

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
        public static NdkInfo Load(XmlReader reader)
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
                        const string DescriptorFileName = "blackberry-sdk-descriptor.xml";

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
                        if (devices == null)
                            devices = new DeviceInfo[0];

                        // try to define info about the installation:
                        return new NdkInfo(name, version, hostPath, targetPath, devices);
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

            // list all configuration files:
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
                                using (var fileReader = new StreamReader(file, Encoding.UTF8))
                                {
                                    using (var reader = XmlReader.Create(fileReader))
                                    {
                                        var info = Load(reader);
                                        if (info != null && info.Exists())
                                        {
                                            var existingIndex = IndexOf(result, info);
                                            
                                            // if NDK info exist with exacly the same paths, prefer to keep the one with longer name:
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

        private static int IndexOf(IEnumerable<NdkInfo> list, NdkInfo info)
        {
            int i = 0;
            foreach (var item in list)
            {
                if (item.Matches(info))
                    return i;
                i++;
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
    }
}
