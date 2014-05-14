using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Win32;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// Short definition of the device used during development.
    /// </summary>
    internal sealed class DeviceDefinition : IComparable<DeviceDefinition>
    {
        private const string FieldDeviceName = "device_name";
        private const string FieldDeviceIP = "device_IP";
        private const string FieldDevicePassword = "device_password";
        private const string FieldSimulatorName = "simulator_name";
        private const string FieldSimulatorIP = "simulator_IP";
        private const string FieldSimulatorPassword = "simulator_password";
        private const string FieldAllDevices = "devices";

        /// <summary>
        /// Init constructor.
        /// </summary>
        public DeviceDefinition(string name, string ip, string password, DeviceDefinitionType type)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException("password");

            Name = name;
            IP = ip;
            Password = password;
            Type = type;
        }

        #region Properties

        public string Name
        {
            get;
            private set;
        }

        public string IP
        {
            get;
            private set;
        }

        public string Password
        {
            get;
            private set;
        }

        public DeviceDefinitionType Type
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets non-null short name of this device.
        /// </summary>
        public string ShortName
        {
            get { return string.IsNullOrEmpty(Name) ? IP : Name; }
        }

        #endregion

        /// <summary>
        /// Checks if given device has specified name.
        /// </summary>
        public bool HasIdenticalName(string name)
        {
            return string.Compare(Name, name, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        /// <summary>
        /// Checks if given device has specified IP.
        /// </summary>
        public bool HasIdenticalIP(string ip)
        {
            return string.Compare(IP, ip, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public int CompareTo(DeviceDefinition other)
        {
            if (other == null)
                return 1;

            if (Type != other.Type)
                return other.Type == DeviceDefinitionType.Device ? 1 : -1;

            int result = string.Compare(IP, other.IP, CultureInfo.CurrentCulture, CompareOptions.IgnoreWidth | CompareOptions.IgnoreCase);
            if (result != 0)
                return result;

            return string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Name))
            {
                return string.Concat(IP, "/", Type == DeviceDefinitionType.Device ? "device" : "simulator", "/");
            }

            return string.Concat(Name, " (", IP, ", ", Type == DeviceDefinitionType.Device ? "device" : "simulator", ")");
        }

        /// <summary>
        /// Finds identical device within given collection.
        /// </summary>
        public static DeviceDefinition Find(DeviceDefinition[] targetDevices, DeviceDefinition device)
        {
            if (device == null || targetDevices == null)
                return null;

            if (!string.IsNullOrEmpty(device.Name))
            {
                // search by name:
                foreach (var d in targetDevices)
                {
                    if (d.Type == device.Type && d.HasIdenticalName(device.Name) && d.HasIdenticalIP(device.IP))
                        return d;
                }
            }

            // in case searching by name failed - try to match by IP:
            foreach (var d in targetDevices)
                if (d.Type == device.Type && d.HasIdenticalIP(device.IP))
                    return d;

            return null;
        }

        /// <summary>
        /// Retrieves device definition from the registry.
        /// </summary>
        public static DeviceDefinition LoadDevice()
        {
            return Load(DeviceDefinitionType.Device);
        }

        /// <summary>
        /// Retrieves simulator definition from the registry.
        /// </summary>
        public static DeviceDefinition LoadSimulator()
        {
            return Load(DeviceDefinitionType.Simulator);
        }

        /// <summary>
        /// Retrieves device definition from registry.
        /// </summary>
        public static DeviceDefinition Load(DeviceDefinitionType type)
        {
            if (type != DeviceDefinitionType.Device && type != DeviceDefinitionType.Simulator)
                throw new ArgumentOutOfRangeException("type", "Unspported device type");

            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings;

            string name = null;
            string ip = null;
            string password = null;

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

            // NAME
            try
            {
                object xName = settings.GetValue(type == DeviceDefinitionType.Device ? FieldDeviceName : FieldSimulatorName);
                if (xName != null)
                    name = xName.ToString();
            }
            catch
            {
            }


            // IP
            try
            {
                object xIP = settings.GetValue(type == DeviceDefinitionType.Device ? FieldDeviceIP : FieldSimulatorIP);
                if (xIP != null)
                    ip = xIP.ToString();
            }
            catch
            {
            }

            // PASSWORD
            try
            {
                object xPassword = settings.GetValue(type == DeviceDefinitionType.Device ? FieldDevicePassword : FieldSimulatorPassword);
                if (xPassword != null)
                    password = GlobalFunctions.Decrypt(xPassword.ToString());
            }
            catch
            {
            }

            settings.Close();
            registry.Close();

            // verify arguments:
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(password))
                return null;

            return new DeviceDefinition(name, ip, password, type);
        }

        /// <summary>
        /// Removes the info about selected device from the registry.
        /// </summary>
        public static void Delete(DeviceDefinitionType type)
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.OpenSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.DeleteSubKey(type == DeviceDefinitionType.Device ? FieldDeviceName : FieldSimulatorName);
                settings.DeleteSubKey(type == DeviceDefinitionType.Device ? FieldDeviceIP : FieldSimulatorIP);
                settings.DeleteSubKey(type == DeviceDefinitionType.Device ? FieldDevicePassword : FieldSimulatorPassword);
            }
            catch (UnauthorizedAccessException)
            {
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Saves given data about the device or simulator into the registry.
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

                if (Name == null)
                    Name = string.Empty;
                if (IP == null)
                    IP = string.Empty;
                if (Password == null)
                    Password = string.Empty;

                settings.SetValue(Type == DeviceDefinitionType.Device ? FieldDeviceName : FieldSimulatorName, Name);
                settings.SetValue(Type == DeviceDefinitionType.Device ? FieldDeviceIP : FieldSimulatorIP, IP);
                settings.SetValue(Type == DeviceDefinitionType.Device ? FieldDevicePassword : FieldSimulatorPassword, GlobalFunctions.Encrypt(Password));
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Loads info about all devices from registry.
        /// </summary>
        public static DeviceDefinition[] LoadAll()
        {
            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.OpenSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return new DeviceDefinition[0];

                return Deserialize((string[])settings.GetValue(FieldAllDevices, null));
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        /// <summary>
        /// Saves info about all devices inside registry.
        /// </summary>
        public static void SaveAll(IEnumerable<DeviceDefinition> devices)
        {
            string[] devicesInfo = Serialize(devices);

            RegistryKey registry = Registry.CurrentUser;
            RegistryKey settings = null;

            try
            {
                settings = registry.CreateSubKey(RunnerDefaults.RegistryPath);
                if (settings == null)
                    return;

                settings.SetValue(FieldAllDevices, devicesInfo, RegistryValueKind.MultiString);
            }
            finally
            {
                if (settings != null)
                    settings.Close();
                registry.Close();
            }
        }

        private static DeviceDefinition[] Deserialize(string[] values)
        {
            if (values == null || values.Length == 0)
                return new DeviceDefinition[0];

            if ((values.Length % 4) != 0)
                throw new ArgumentOutOfRangeException("values");

            var result = new List<DeviceDefinition>();
            for (int i = 0; i < values.Length; i += 4)
            {
                var device = new DeviceDefinition(string.IsNullOrEmpty(values[i]) ? null : values[i],
                                                  values[i + 2], GlobalFunctions.Decrypt(values[i + 3]),
                                                  values[i + 1] == "S" ? DeviceDefinitionType.Simulator : DeviceDefinitionType.Device);
                result.Add(device);
            }

            return result.ToArray();
        }

        private static string[] Serialize(IEnumerable<DeviceDefinition> devices)
        {
            var result = new List<string>();

            if (devices != null)
            {
                foreach (var device in devices)
                {
                    result.Add(device.Name ?? string.Empty);
                    result.Add(device.Type == DeviceDefinitionType.Device ? "D" : "S");
                    result.Add(device.IP);
                    result.Add(GlobalFunctions.Encrypt(device.Password));
                }
            }

            return result.ToArray();
        }
    }
}
