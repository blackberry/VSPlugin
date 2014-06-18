using System;
using System.IO;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;

namespace BlackBerry.Package.ViewModels
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
        private SimulatorInfo[] _installedSimulators;
        private RuntimeInfo[] _installedRuntimes;
        private ApiInfoArray[] _remoteNDKs;
        private ApiInfoArray[] _remoteSimulators;
        private ApiInfoArray[] _remoteRuntimes;
        private NdkInfo _activeNDK;
        private DeviceDefinition[] _targetDevices;
        private DeviceDefinition _activeDevice;
        private DeviceDefinition _activeSimulator;

        public PackageViewModel()
        {
            _remoteNDKs = new ApiInfoArray[0];
            _remoteSimulators = new ApiInfoArray[0];
            _remoteRuntimes = new ApiInfoArray[0];
            UpdateManager = new UpdateManager(RunnerDefaults.NdkDirectory);
            UpdateManager.Completed += UpdateManagerOnCompleted;
        }

        #region Properties

        /// <summary>
        /// Gets the description of current developer (publisher).
        /// </summary>
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

        /// <summary>
        /// Gets the reference to the UpdateManager, responsible for adding and removing features (like install new NDK, simulator or runtime)
        /// </summary>
        public UpdateManager UpdateManager
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the list of installed NDKs on the machine.
        /// </summary>
        public NdkInfo[] InstalledNDKs
        {
            get
            {
                if (_installedNDKs == null)
                {
                    // load info about NDKs from specified locations:
                    _installedNDKs = NdkInfo.Load(RunnerDefaults.PluginInstallationConfigDirectory,
                                                  RunnerDefaults.SupplementaryInstallationConfigDirectory,
                                                  RunnerDefaults.InstallationConfigDirectory,
                                                  RunnerDefaults.SupplementaryPlayBookInstallationConfigDirectory);
                }

                return _installedNDKs;
            }
        }

        /// <summary>
        /// Gets the cached list of NDKs available on-line to install.
        /// </summary>
        public ApiInfoArray[] RemoteNDKs
        {
            get { return _remoteNDKs; }
            set { _remoteNDKs = value ?? new ApiInfoArray[0]; }
        }

        /// <summary>
        /// Gets the list of installed simulators.
        /// </summary>
        public SimulatorInfo[] InstalledSimulators
        {
            get
            {
                if (_installedSimulators == null)
                {
                    // load info about simulators from specified locations:
                    _installedSimulators = SimulatorInfo.Load(RunnerDefaults.NdkDirectory);
                }

                return _installedSimulators;
            }
        }

        /// <summary>
        /// Gets the cached list of simulators available on-line to install.
        /// </summary>
        public ApiInfoArray[] RemoteSimulators
        {
            get { return _remoteSimulators; }
            set { _remoteSimulators = value ?? new ApiInfoArray[0]; }
        }

        /// <summary>
        /// Gets the list of installed runtimes.
        /// </summary>
        public RuntimeInfo[] InstalledRuntimes
        {
            get
            {
                if (_installedRuntimes == null)
                {
                    // load info about runtimes from specified locations:
                    _installedRuntimes = RuntimeInfo.Load(RunnerDefaults.NdkDirectory);
                }

                return _installedRuntimes;
            }
        }

        /// <summary>
        /// Gets the cached list of runtimes available on-line to install.
        /// </summary>
        public ApiInfoArray[] RemoteRuntimes
        {
            get { return _remoteRuntimes; }
            set { _remoteRuntimes = value ?? new ApiInfoArray[0]; }
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
                if (value != null && !value.Matches(_activeNDK) && InstalledNDKs.Length > 0)
                {
                    var index = IndexOfInstalled(value);
                    if (index >= 0)
                    {
                        _activeNDK = _installedNDKs[index];
                        TraceLog.WriteLine("Changed active NDK to: \"{0}\"", _activeNDK);
                        SaveActiveNDK();
                    }
                    else
                    {
                        if (_activeNDK != null)
                        {
                            TraceLog.WriteLine("Removed active NDK: \"{0}\"", _activeNDK);
                            _activeNDK = null;
                            SaveActiveNDK();
                        }
                    }
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
        /// Gets an index of installed NDK with identical version.
        /// </summary>
        public int IndexOfInstalledNDK(Version version)
        {
            return ApiInfo.IndexOf(InstalledNDKs, version);
        }

        /// <summary>
        /// Gets an index of installed simulator with identical version.
        /// </summary>
        public int IndexOfInstalledSimulator(Version version)
        {
            return ApiInfo.IndexOf(InstalledSimulators, version);
        }

        /// <summary>
        /// Gets an index of installed runtime with identical version.
        /// </summary>
        public int IndexOfInstalledRuntime(Version version)
        {
            return ApiInfo.IndexOf(InstalledRuntimes, version);
        }

        /// <summary>
        /// Persists info about currently selected NDK into the registry.
        /// </summary>
        private void SaveActiveNDK()
        {
            if (_activeNDK == null || !_activeNDK.IsInstalled)
            {
                NdkDefinition.Delete();
                TraceLog.WarnLine("Invalid NDK to set as active!");
                return;
            }

            try
            {
                var setting = new NdkDefinition(_activeNDK.HostPath, _activeNDK.TargetPath, _activeNDK.Type);
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

        /// <summary>
        /// Resets the cached list of simulators, allowing to reload them again.
        /// </summary>
        public void ResetSimulators()
        {
            _installedSimulators = null;
        }

        /// <summary>
        /// Resets the cached list of runtimes, allowing to reload them again.
        /// </summary>
        public void ResetRuntimes()
        {
            _installedRuntimes = null;
        }

        /// <summary>
        /// Resets the cached list of given type.
        /// </summary>
        public void Reset(ApiLevelTarget target)
        {
            switch (target)
            {
                case ApiLevelTarget.NDK:
                    ResetNDKs();
                    break;
                case ApiLevelTarget.Simulator:
                    ResetSimulators();
                    break;
                case ApiLevelTarget.Runtime:
                    ResetRuntimes();
                    break;
                default:
                    throw new InvalidOperationException("Unsupported target type (" + target + ")");
            }
        }

        /// <summary>
        /// Removes custom reference to existing NDK, created by developer some time ago.
        /// </summary>
        public void Forget(NdkInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            if (string.IsNullOrEmpty(info.FilePath))
                return;

            try
            {
                if (File.Exists(info.FilePath))
                    File.Delete(info.FilePath);
                ResetNDKs();
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Problem removing file: \"{0}\"", info.FilePath);
            }
        }

        private void UpdateManagerOnCompleted(object sender, UpdateManagerCompletedEventArgs e)
        {
            Reset(e.Target);
        }

        /// <summary>
        /// Makes sure any BlackBerry 10 NDK is selected and its paths are stored inside registry.
        /// </summary>
        public void EnsureActiveNDK()
        {
            // need to select anything?
            if (ActiveNDK == null)
            {
                ActiveNDK = GetLatestNDK();
            }
            else
            {
                // make sure invalid info from registry is removed:
                NdkDefinition.Delete();
            }
        }

        /// <summary>
        /// Gets the reference to the latest version of the NDK installed locally.
        /// </summary>
        private NdkInfo GetLatestNDK()
        {
            // get the last one, as this list is already sorted by version:
            int length = InstalledNDKs.Length;
            if (length > 0)
            {
                return InstalledNDKs[length - 1];
            }

            return null;
        }
    }
}
