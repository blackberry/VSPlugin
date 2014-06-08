using System;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class ApiLevelOptionViewModel : IDisposable
    {
        #region Internal Classes

        private abstract class LoaderViewModel : IDisposable
        {
            /// <summary>
            /// Event fired, when data is loaded.
            /// </summary>
            public event EventHandler Loaded;

            private ApiLevelListLoadRunner _runner;
            private readonly ApiLevelListTypes _type;

            #region Properties

            public abstract ApiInfoArray[] Remote
            {
                get;
                protected set;
            }

            #endregion

            public LoaderViewModel(ApiLevelListTypes type)
            {
                _type = type;
            }

            ~LoaderViewModel()
            {
                Dispose(false);
            }

            public bool Load(bool reload, IEventDispatcher dispatcher)
            {
                // is it still loading?
                if (_runner != null)
                    return true;

                // was it loaded before?
                if (!reload && Remote != null && Remote.Length > 0)
                {
                    NotifyListLoaded();
                    return false;
                }

                _runner = new ApiLevelListLoadRunner(RunnerDefaults.NdkDirectory, _type);
                _runner.Dispatcher = dispatcher;
                _runner.Finished += RunnerOnFinished;
                _runner.ExecuteAsync();
                return true;
            }

            private void RunnerOnFinished(object sender, ToolRunnerEventArgs e)
            {
                Remote = _runner.ApiLevels;
                _runner.Finished -= RunnerOnFinished;
                _runner = null;

                NotifyListLoaded();
            }

            private void NotifyListLoaded()
            {
                var handler = Loaded;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }

            public ApiLevelActionType GetAction(ApiInfo info)
            {
                object argument;
                return GetAction(info, out argument);
            }

            public abstract ApiLevelActionType GetAction(ApiInfo info, out object argument);

            public abstract bool IsInstalled(ApiInfo info);

            public abstract bool IsProcessing(ApiInfo info);

            #region IDisposable Implementation

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_runner != null)
                    {
                        _runner.Dispose();
                        _runner = null;
                    }
                }
            }

            #endregion
        }

        private sealed class NdkLoaderViewModel : LoaderViewModel
        {
            private readonly static Version LastPlayBookVersion = new Version(9, 99, 0, 0);

            public NdkLoaderViewModel()
                : base(ApiLevelListTypes.Full)
            {
            }

            public override ApiInfoArray[] Remote
            {
                get { return PackageViewModel.Instance.RemoteNDKs; }
                protected set { PackageViewModel.Instance.RemoteNDKs = value; }
            }

            public NdkInfo[] Installed
            {
                get { return PackageViewModel.Instance.InstalledNDKs; }
            }

            public NdkInfo Active
            {
                get { return PackageViewModel.Instance.ActiveNDK; }
                set { PackageViewModel.Instance.ActiveNDK = value; }
            }

            public override ApiLevelActionType GetAction(ApiInfo info, out object argument)
            {
                argument = null;

                // nothing should be displayed for not defined NDK:
                if (info == null)
                    return ApiLevelActionType.Hide;

                // check, if it exists on disk:
                bool isInstalled = IsInstalled(info) || AreItemsInstalled(info as ApiInfoArray);
                var ndkInfo = info as NdkInfo;

                if (ndkInfo != null)
                {
                    // is it NDK owned by the plugin itself?
                    if (isInstalled && ndkInfo.TargetPath != null && ndkInfo.TargetPath.StartsWith(RunnerDefaults.NdkDirectory, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return IsProcessing(info) ? ApiLevelActionType.Nothing : ApiLevelActionType.Uninstall;
                    }

                    // is it a custom definition added?
                    if (ndkInfo.FilePath != null && ndkInfo.FilePath.StartsWith(RunnerDefaults.PluginInstallationConfigDirectory, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return ApiLevelActionType.Forget;
                    }
                }

                // is it a PlayBook NDK?
                if (info.Version <= LastPlayBookVersion)
                {
                    if (isInstalled)
                        return ApiLevelActionType.Nothing;

                    argument = "http://developer.blackberry.com/playbook/native/download/";
                    return ApiLevelActionType.InstallManually;
                }

                // by default, check, if it can be installed, or it's not owned by the plugin:
                return isInstalled ? ApiLevelActionType.Nothing : ApiLevelActionType.Install;
            }

            public override bool IsInstalled(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.IndexOfInstalledNDK(info.Version) >= 0;
            }

            public override bool IsProcessing(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.UpdateManager.IsProcessing(info.Version, UpdateActionTargets.NDK);
            }

            /// <summary>
            /// Checks, if all items owned by an array are already installed.
            /// </summary>
            private bool AreItemsInstalled(ApiInfoArray definition)
            {
                if (definition == null)
                    return false;

                foreach (var item in definition.Items)
                {
                    if (!IsInstalled(item))
                        return false;
                }

                return true;
            }

            public void ReloadAndActivate(NdkInfo ndk)
            {
                PackageViewModel.Instance.ResetNDKs();
                Active = ndk;
            }
        }

        private sealed class SimulatorLoaderViewModel : LoaderViewModel
        {
            public SimulatorLoaderViewModel()
                : base(ApiLevelListTypes.Simulators)
            {
            }

            public override ApiInfoArray[] Remote
            {
                get { return PackageViewModel.Instance.RemoteSimulators; }
                protected set { PackageViewModel.Instance.RemoteSimulators = value; }
            }

            public SimulatorInfo[] Installed
            {
                get { return PackageViewModel.Instance.InstalledSimulators; }
            }

            public override ApiLevelActionType GetAction(ApiInfo info, out object argument)
            {
                argument = null;

                // nothing should be displayed for invalid simulator:
                if (info == null)
                    return ApiLevelActionType.Hide;

                // check, if it exists on disk:
                var infoArray = info as ApiInfoArray;
                bool isInstalled = IsInstalled(info) || (infoArray != null && infoArray.IsInstalled);
                var simulatorInfo = info as SimulatorInfo;

                if (simulatorInfo != null)
                {
                    return isInstalled && !IsProcessing(info) ? ApiLevelActionType.Uninstall : ApiLevelActionType.Nothing;
                }

                return isInstalled ? ApiLevelActionType.Nothing : ApiLevelActionType.Install;
            }

            public override bool IsInstalled(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.IndexOfInstalledSimulator(info.Version) >= 0;
            }

            public override bool IsProcessing(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.UpdateManager.IsProcessing(info.Version, UpdateActionTargets.Simulator);
            }
        }

        private sealed class RuntimeLoaderViewModel : LoaderViewModel
        {
            public RuntimeLoaderViewModel()
                : base(ApiLevelListTypes.Runtimes)
            {
            }

            public override ApiInfoArray[] Remote
            {
                get { return PackageViewModel.Instance.RemoteRuntimes; }
                protected set { PackageViewModel.Instance.RemoteRuntimes = value; }
            }

            public RuntimeInfo[] Installed
            {
                get { return PackageViewModel.Instance.InstalledRuntimes; }
            }

            public override ApiLevelActionType GetAction(ApiInfo info, out object argument)
            {
                argument = null;

                // nothing should be displayed for invalid runtime libraries:
                if (info == null)
                    return ApiLevelActionType.Hide;

                // check, if it exists on disk:
                var infoArray = info as ApiInfoArray;
                bool isInstalled = IsInstalled(info) || (infoArray != null && infoArray.IsInstalled);
                var runtimeInfo = info as RuntimeInfo;

                if (runtimeInfo != null)
                {
                    return isInstalled && !IsProcessing(info) ? ApiLevelActionType.Uninstall : ApiLevelActionType.Nothing;
                }

                return isInstalled ? ApiLevelActionType.Nothing : ApiLevelActionType.Install;
            }

            public override bool IsInstalled(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.IndexOfInstalledRuntime(info.Version) >= 0;
            }

            public override bool IsProcessing(ApiInfo info)
            {
                return info != null && PackageViewModel.Instance.UpdateManager.IsProcessing(info.Version, UpdateActionTargets.Runtime);
            }
        }

        #endregion

        public event EventHandler NdkListLoaded
        {
            add { _ndk.Loaded += value; }
            remove { _ndk.Loaded -= value; }
        }

        public event EventHandler SimulatorListLoaded
        {
            add { _simulator.Loaded += value; }
            remove { _simulator.Loaded -= value; }
        }

        public event EventHandler RuntimeListLoaded
        {
            add { _runtime.Loaded += value; }
            remove { _runtime.Loaded -= value; }
        }

        private readonly NdkLoaderViewModel _ndk;
        private readonly SimulatorLoaderViewModel _simulator;
        private readonly RuntimeLoaderViewModel _runtime;

        public ApiLevelOptionViewModel()
        {
            _ndk = new NdkLoaderViewModel();
            _simulator = new SimulatorLoaderViewModel();
            _runtime = new RuntimeLoaderViewModel();
        }

        ~ApiLevelOptionViewModel()
        {
            Dispose(false);
        }

        public NdkInfo[] InstalledNDKs
        {
            get { return _ndk.Installed; }
        }

        public ApiInfoArray[] RemoteNDKs
        {
            get { return _ndk.Remote; }
        }

        public SimulatorInfo[] InstalledSimulators
        {
            get { return _simulator.Installed; }
        }

        public ApiInfoArray[] RemoteSimulators
        {
            get { return  _simulator.Remote; }
        }

        public RuntimeInfo[] InstalledRuntimes
        {
            get { return _runtime.Installed; }
        }

        public ApiInfoArray[] RemoteRuntimes
        {
            get { return _runtime.Remote; }
        }

        public NdkInfo ActiveNDK
        {
            get { return _ndk.Active; }
            set { _ndk.Active = value; }
        }

        public IEventDispatcher Dispatcher
        {
            get;
            set;
        }

        public void ReloadAndActivate(NdkInfo ndk)
        {
            _ndk.ReloadAndActivate(ndk);
        }

        public ApiLevelActionType GetAction(ApiInfo info, UpdateActionTargets target)
        {
            switch (target)
            {
                case UpdateActionTargets.NDK:
                    return _ndk.GetAction(info);
                case UpdateActionTargets.Simulator:
                    return _simulator.GetAction(info);
                case UpdateActionTargets.Runtime:
                    return _runtime.GetAction(info);
                default:
                    throw new ArgumentOutOfRangeException("target");
            }
        }

        public ApiLevelActionType GetAction(ApiInfo info, UpdateActionTargets target, out object argument)
        {
            switch (target)
            {
                case UpdateActionTargets.NDK:
                    return _ndk.GetAction(info, out argument);
                case UpdateActionTargets.Simulator:
                    return _simulator.GetAction(info, out argument);
                case UpdateActionTargets.Runtime:
                    return _runtime.GetAction(info, out argument);
                default:
                    throw new ArgumentOutOfRangeException("target");
            }
        }

        /// <summary>
        /// Loads the list of remote NDKs.
        /// If data is already available this method will return 'false'. 'True', if loading in progress.
        /// </summary>
        public bool Load(UpdateActionTargets target, bool reload)
        {
            switch (target)
            {
                case UpdateActionTargets.NDK:
                    return _ndk.Load(reload, Dispatcher);
                case UpdateActionTargets.Simulator:
                    return _simulator.Load(reload, Dispatcher);
                case UpdateActionTargets.Runtime:
                    return _runtime.Load(reload, Dispatcher);
                default:
                    throw new ArgumentOutOfRangeException("target");
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ndk.Dispose();
                _simulator.Dispose();
                _runtime.Dispose();
            }
        }

        #endregion

        public bool CheckIfInstalled(ApiInfo info, UpdateActionTargets target)
        {
            switch (target)
            {
                case UpdateActionTargets.NDK:
                    return _ndk.IsInstalled(info);
                case UpdateActionTargets.Simulator:
                    return _simulator.IsInstalled(info);
                case UpdateActionTargets.Runtime:
                    return _runtime.IsInstalled(info);
                default:
                    throw new ArgumentOutOfRangeException("target");
            }
        }

        public bool IsProcessing(ApiInfo info, UpdateActionTargets target)
        {
            switch (target)
            {
                case UpdateActionTargets.NDK:
                    return _ndk.IsProcessing(info);
                case UpdateActionTargets.Simulator:
                    return _simulator.IsProcessing(info);
                case UpdateActionTargets.Runtime:
                    return _runtime.IsProcessing(info);
                default:
                    throw new ArgumentOutOfRangeException("target");
            }
        }

        /// <summary>
        /// Requests to download and install something.
        /// </summary>
        public void RequestInstall(ApiInfo info, UpdateActionTargets target)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            PackageViewModel.Instance.UpdateManager.Request(UpdateActions.Install, target, info.Version);
        }

        /// <summary>
        /// Requests to uninstall and remove something.
        /// </summary>
        public void RequestRemoval(ApiInfo info, UpdateActionTargets target)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            PackageViewModel.Instance.UpdateManager.Request(UpdateActions.Uninstall, target, info.Version);
        }

        /// <summary>
        /// Removes the reference, created by developer some time ago.
        /// </summary>
        public void Forget(ApiInfo info)
        {
            var ndkInfo = info as NdkInfo;

            if (ndkInfo != null)
            {
                PackageViewModel.Instance.Forget(ndkInfo);
            }
        }
    }
}
