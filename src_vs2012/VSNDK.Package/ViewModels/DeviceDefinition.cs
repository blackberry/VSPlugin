using System;
using Microsoft.Win32;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// Short definition of the device used during development.
    /// </summary>
    internal sealed class DeviceDefinition
    {
        private const string FieldDeviceName = "device_name";
        private const string FieldDeviceIP = "device_IP";
        private const string FieldDevicePassword = "device_password";
        private const string FieldSimulatorName = "simulator_name";
        private const string FieldSimulatorIP = "simulator_IP";
        private const string FieldSimulatorPassword = "simulator_password";

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

        #endregion

        /// <summary>
        /// Retrieves device definition from the registry.
        /// </summary>
        public static DeviceDefinition LoadDeviceInfo()
        {
            return LoadInfo(DeviceDefinitionType.Device);
        }

        /// <summary>
        /// Retrieves simulator definition from the registry.
        /// </summary>
        public static DeviceDefinition LoadSimulatorInfo()
        {
            return LoadInfo(DeviceDefinitionType.Simulator);
        }

        /// <summary>
        /// Retrieves device definition from registry.
        /// </summary>
        public static DeviceDefinition LoadInfo(DeviceDefinitionType type)
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
        /// Saves given data about the device or simulator into the registry.
        /// </summary>
        public void SaveInfo()
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
            catch
            {

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
