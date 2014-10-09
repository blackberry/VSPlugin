using System.Runtime.InteropServices;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Services
{
    /// <summary>
    /// Interface exposed by components providing find-a-running-device feature.
    /// </summary>
    [Guid("0a034a4d-dfe3-405d-86c9-b47602813df8")]
    public interface IDeviceDiscoveryService
    {
        /// <summary>
        /// Do anything what needed to allow developer to find a device and return it or null, if cancelled or not found.
        /// </summary>
        DeviceDefinition FindDevice();
    }
}
