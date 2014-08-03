using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class with DeviceFamilyType methods.
    /// </summary>
    public sealed class DeviceHelper
    {
        /// <summary>
        /// Gets the string representation of the DeviceDefinitionType.
        /// </summary>
        public static string GetTypeToString(DeviceDefinitionType type)
        {
            switch (type)
            {
                case DeviceDefinitionType.Simulator:
                    return "simulator";
                case DeviceDefinitionType.Device:
                    return "device";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Gets the DeviceDefinitionType from string.
        /// </summary>
        public static DeviceDefinitionType GetTypeFromString(string name)
        {
            if (string.IsNullOrEmpty(name))
                return DeviceDefinitionType.Device;

            if (string.Compare("simulator", name, StringComparison.OrdinalIgnoreCase) == 0)
                return DeviceDefinitionType.Simulator;
            if (string.Compare("sim", name, StringComparison.OrdinalIgnoreCase) == 0)
                return DeviceDefinitionType.Simulator;
            if(name == "S" || name == "s")
                return DeviceDefinitionType.Simulator;

            return DeviceDefinitionType.Device;
        }

        /// <summary>
        /// Gets the string representation of the DeviceFamilyType.
        /// </summary>
        public static string GetFamilyTypeToString(DeviceFamilyType type)
        {
            switch (type)
            {
                case DeviceFamilyType.Phone:
                    return "phone";
                case DeviceFamilyType.Tablet:
                    return "tablet";
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        /// <summary>
        /// Gets the DeviceFamilyType from string.
        /// </summary>
        public static DeviceFamilyType GetFamilyTypeFromString(string name)
        {
            if (string.IsNullOrEmpty(name))
                return DeviceFamilyType.Phone;

            if (string.Compare("tablet", name, StringComparison.OrdinalIgnoreCase) == 0)
                return DeviceFamilyType.Tablet;

            return DeviceFamilyType.Phone;
        }
    }
}
