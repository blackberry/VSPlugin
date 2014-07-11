using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class with DeviceFamilyType methods.
    /// </summary>
    internal sealed class DeviceFamilyHelper
    {
        /// <summary>
        /// Gets the string representation of the DeviceFamilyType.
        /// </summary>
        public static string GetTypeToString(DeviceFamilyType type)
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
        public static DeviceFamilyType GetTypeFromString(string name)
        {
            if (string.IsNullOrEmpty(name))
                return DeviceFamilyType.Phone;

            if (string.Compare("tablet", name, StringComparison.OrdinalIgnoreCase) == 0)
                return DeviceFamilyType.Tablet;

            return DeviceFamilyType.Phone;
        }
    }
}
