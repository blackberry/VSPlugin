using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.Package.ViewModels
{
    /// <summary>
    /// View model for editing list of target devices.
    /// </summary>
    internal sealed class TargetsOptionViewModel
    {
        private List<DeviceDefinition> _devices;
        private DeviceDefinition _activeDevice;
        private DeviceDefinition _activeSimulator;
        private Dictionary<DeviceDefinition, DeviceInfo> _details;

        /// <summary>
        /// Gets the reference to current developer.
        /// </summary>
        public DeveloperDefinition Developer
        {
            get { return PackageViewModel.Instance.Developer; }
        }

        /// <summary>
        /// Gets the list of target devices.
        /// </summary>
        public ICollection<DeviceDefinition> Devices
        {
            get
            {
                if (_devices == null)
                {
                    // start from the global list of known devies:
                    _devices = new List<DeviceDefinition>(PackageViewModel.Instance.TargetDevices);
                    _activeDevice = PackageViewModel.Instance.ActiveDevice;
                    _activeSimulator = PackageViewModel.Instance.ActiveSimulator;
                    UpdateRealDevicesCount();
                }

                return _devices;
            }
        }

        public DeviceDefinition ActiveDevice
        {
            get { return _activeDevice; }
        }

        public int RealDevicesCount
        {
            get;
            private set;
        }

        private void UpdateRealDevicesCount()
        {
            int count = 0;

            if (_devices != null)
            {
                foreach (var d in _devices)
                {
                    if (d.Type == DeviceDefinitionType.Device)
                        count++;
                }
            }

            RealDevicesCount = count;
        }

        public bool IsActive(DeviceDefinition device)
        {
            if (device == null)
                return false;

            return device == _activeDevice || device == _activeSimulator;
        }

        public void Add(DeviceDefinition device)
        {
            if (device == null)
                return;

            _devices.Add(device);
            _devices.Sort();
            UpdateRealDevicesCount();

            // and set as activate automatically:
            if (_activeDevice == null && device.Type == DeviceDefinitionType.Device)
            {
                _activeDevice = device;
            }
            else
            {
                if (_activeSimulator == null && device.Type == DeviceDefinitionType.Simulator)
                {
                    _activeSimulator = device;
                }
            }
        }

        public void SetActive(DeviceDefinition device)
        {
            if (device == null)
                return;

            // if is already active, then 'deactivate' it:
            if (_activeDevice == device)
            {
                _activeDevice = null;
                return;
            }

            if (_activeSimulator == device)
            {
                _activeSimulator = null;
                return;
            }

            // ok, just activate it:
            if (device.Type == DeviceDefinitionType.Device)
                _activeDevice = device;
            else
                _activeSimulator = device;
        }

        public void Update(DeviceDefinition oldDevice, DeviceDefinition newDevice)
        {
            // try to detect other operations smuggled by this one:
            if (oldDevice == null)
            {
                Add(newDevice);
                return;
            }

            if (newDevice == null)
            {
                Remove(oldDevice);
                return;
            }

            // ok, replace the instance and 'active' markers:
            int index = _devices.IndexOf(oldDevice);
            if (index < 0)
                throw new InvalidOperationException("Invalid Target Device info to update");

            _devices[index] = newDevice;
            _devices.Sort();
            UpdateRealDevicesCount();

            if (_activeDevice == oldDevice)
            {
                _activeDevice = newDevice;
            }
            else
            {
                if (_activeSimulator == oldDevice)
                {
                    _activeSimulator = newDevice;
                }
            }
        }

        public void Remove(DeviceDefinition device)
        {
            if (device == null)
                return;

            _devices.Remove(device);
            UpdateRealDevicesCount();

            if (_activeDevice == device)
            {
                _activeDevice = null;
            }
            else
            {
                if (_activeSimulator == device)
                {
                    _activeSimulator = null;
                }
            }
        }

        /// <summary>
        /// Gets detailed physical device description if available.
        /// </summary>
        public DeviceInfo GetDetails(DeviceDefinition device)
        {
            if (device == null)
                return null;
            if (_details == null)
                return null;

            DeviceInfo details;
            return _details.TryGetValue(device, out details) ? details : null;
        }

        /// <summary>
        /// Sets or updates detailed info about particular device.
        /// </summary>
        public void SetDetails(DeviceDefinition device, DeviceInfo details)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            if (details == null)
            {
                if (_details != null)
                    _details.Remove(device);
            }
            else
            {
                if (_details == null)
                    _details = new Dictionary<DeviceDefinition, DeviceInfo>();

                // add or update info:
                _details[device] = details;
            }
        }

        /// <summary>
        /// OK. Save it.
        /// </summary>
        public void Apply()
        {
            PackageViewModel.Instance.TargetDevices = _devices.ToArray();
            PackageViewModel.Instance.ActiveDevice = _activeDevice;
            PackageViewModel.Instance.ActiveSimulator = _activeSimulator;
        }

        /// <summary>
        /// User cancelled settings.
        /// </summary>
        public void Reset()
        {
            _devices = null;
            _activeDevice = null;
            _activeSimulator = null;
            _details = null;
        }

        /// <summary>
        /// Updates the cached info about author (publisher).
        /// </summary>
        public void Update(AuthorInfo info)
        {
            // do it instantly:
            PackageViewModel.Instance.UpdateCachedAuthor(info);
        }
    }
}
