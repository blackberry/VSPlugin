using System;
using System.Collections.Generic;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// View model for editing list of target devices.
    /// </summary>
    internal sealed class TargetsOptionViewModel
    {
        private List<DeviceDefinition> _devices;
        private DeviceDefinition _activeDevice;
        private DeviceDefinition _activeSimulator;

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
                }

                return _devices;
            }
        }

        public DeviceDefinition ActiveDevice
        {
            get { return _activeDevice; }
        }

        public bool IsSelected(DeviceDefinition device)
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
        /// OK. Save it.
        /// </summary>
        public void Apply()
        {
            PackageViewModel.Instance.TargetDevices = _devices.ToArray();
            PackageViewModel.Instance.ActiveDevice = _activeDevice;
            PackageViewModel.Instance.ActiveSimulator = _activeSimulator;
        }
    }
}
