using System;
using RIM.VSNDK_Package.Diagnostics;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    /// <summary>
    /// This is a global view-model, that keeps track and caches all the data.
    /// </summary>
    internal sealed class PackageViewModel
    {
        #region Singleton

        private static PackageViewModel _instance;

        /// <summary>
        /// Gets the instance of the ViewModel.
        /// </summary>
        public static PackageViewModel Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PackageViewModel();
                return _instance;
            }
        }

        #endregion

        private DeveloperDefinition _developer;
        private NdkInfo[] _installedNDKs;
        private NdkInfo _activeNDK;
        private DeviceDefinition[] _targetDevices;
        private DeviceDefinition _activeDevice;
        private DeviceDefinition _activeSimulator;

        public PackageViewModel()
        {
        }

        #region Properties

        public DeveloperDefinition Developer
        {
            get
            {
                if (_developer == null)
                {
                    // load info about current developer:
                    _developer = DeveloperDefinition.Load(RunnerDefaults.DataDirectory);
                }

                return _developer;
            }
        }

        public NdkInfo[] InstalledNDKs
        {
            get
            {
                if (_installedNDKs == null)
                {
                    // load info about NDKs from specified locations:
                    _installedNDKs = NdkInfo.Load(RunnerDefaults.InstallationConfigDirectory,
                                                  RunnerDefaults.SupplementaryInstallationConfigDirectory,
                                                  RunnerDefaults.SupplementaryPlayBookInstallationConfigDirectory);
                }

                return _installedNDKs;
            }
        }

        /// <summary>
        /// Loads info about currently used NDK from registry.
        /// </summary>
        public NdkInfo ActiveNDK
        {
            get
            {
                if (_activeNDK != null)
                    return _activeNDK;

                // or try to find it:
                var settings = NdkDefinition.Load();

                if (settings == null)
                    return null;

                // find matching installed NDK by comparing paths:
                foreach(var info in InstalledNDKs)
                    if (info.Matches(settings.HostPath, settings.TargetPath))
                    {
                        _activeNDK = info;

                        TraceLog.WriteLine("Found active NDK: \"{0}\"", _activeNDK);
                        return info;
                    }

                return null;
            }
            set
            {
                if (value != null && !value.Matches(_activeNDK))
                {
                    var index = IndexOfInstalled(value);
                    if (index < 0)
                        throw new ArgumentOutOfRangeException("value", "Invalid value set, it must belong to the InstalledNDKs first");

                    _activeNDK = _installedNDKs[index];
                    TraceLog.WriteLine("Changed active NDK to: \"{0}\"", _activeNDK);
                    SaveActiveNDK();
                }
            }
        }

        /// <summary>
        /// Gets the list of all available target devices.
        /// </summary>
        public DeviceDefinition[] TargetDevices
        {
            get
            {
                if (_targetDevices == null)
                {
                    _targetDevices = DeviceDefinition.LoadAll();

                    var device = DeviceDefinition.LoadDevice();
                    var simulator = DeviceDefinition.LoadSimulator();

                    _activeDevice = DeviceDefinition.Find(_targetDevices, device);
                    _activeSimulator = DeviceDefinition.Find(_targetDevices, simulator);

                    TraceLog.WriteLine("Loaded list of Target Devices");
                    TraceLog.WriteLine("Found active Target Devices:");
                    TraceLog.WriteLine(" * device - {0}", _activeDevice != null ? _activeDevice.ToString() : "none");
                    TraceLog.WriteLine(" * simulator - {0}", _activeSimulator != null ? _activeSimulator.ToString() : "none");
                }

                return _targetDevices;
            }
            set
            {
                _targetDevices = value ?? new DeviceDefinition[0];
                DeviceDefinition.SaveAll(_targetDevices);

                ActiveDevice = DeviceDefinition.Find(_targetDevices, _activeDevice);
                ActiveSimulator = DeviceDefinition.Find(_targetDevices, _activeSimulator);
            }
        }

        /// <summary>
        /// Gets the reference to currently active device.
        /// </summary>
        public DeviceDefinition ActiveDevice
        {
            get { return _activeDevice; }
            set
            {
                // check, if type is expected:
                if (value != null && value.Type == DeviceDefinitionType.Simulator)
                {
                    ActiveSimulator = value;
                    return;
                }

                // update field and store it:
                if (_activeDevice != value)
                {
                    _activeDevice = value;

                    if (_activeDevice == null)
                    {
                        DeviceDefinition.Delete(DeviceDefinitionType.Device);
                        TraceLog.WarnLine("No Target Device is active now");
                    }
                    else
                    {
                        _activeDevice.Save();
                        TraceLog.WriteLine("Set active Target Device: {0}", _activeDevice);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the reference to currently active simulator.
        /// </summary>
        public DeviceDefinition ActiveSimulator
        {
            get { return _activeSimulator; }
            set
            {
                // check if type is as expected:
                if (value != null && value.Type == DeviceDefinitionType.Device)
                {
                    ActiveDevice = value;
                    return;
                }

                // update field and store it:
                if (_activeSimulator != value)
                {
                    _activeSimulator = value;
                    if (_activeSimulator == null)
                    {
                        DeviceDefinition.Delete(DeviceDefinitionType.Simulator);
                        TraceLog.WarnLine("No Target Simulator is active now");
                    }
                    else
                    {
                        _activeSimulator.Save();
                        TraceLog.WriteLine("Set active Target Device: {0}", _activeDevice);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets an index of identical installed NDK.
        /// </summary>
        public int IndexOfInstalled(NdkInfo info)
        {
            return NdkInfo.IndexOf(InstalledNDKs, info);
        }

        /// <summary>
        /// Persists info about currently selected NDK into the registry.
        /// </summary>
        private void SaveActiveNDK()
        {
            if (_activeNDK == null || !_activeNDK.Exists())
            {
                TraceLog.WarnLine("Invalid NDK to set as active!");
                return;
            }

            try
            {
                var setting = new NdkDefinition(_activeNDK.HostPath, _activeNDK.TargetPath);
                setting.Save();
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to activate NDK \"{0}\"", _activeNDK);
            }
        }

        /// <summary>
        /// Removes info about selected NDK from the registry.
        /// </summary>
        public void DeleteActiveNDK()
        {
            try
            {
                NdkDefinition.Delete();
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to clear info about active NDK");
            }
        }

        /// <summary>
        /// Resets the cached lists of NDKs, allowing to reload them again.
        /// </summary>
        public void ResetNDKs()
        {
            _installedNDKs = null;
            _activeNDK = null;
        }
    }
}
