using System;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class ApiLevelOptionViewModel : IDisposable
    {
        public event EventHandler NdkListLoaded;

        private readonly static Version LastPlayBookVersion = new Version(9, 99, 0, 0);
        private ApiLevelListLoadRunner _ndkLoadRunner;

        ~ApiLevelOptionViewModel()
        {
            Dispose(false);
        }

        public NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
        }

        public ApiInfoArray[] RemoteNDKs
        {
            get { return PackageViewModel.Instance.RemoteNDKs; }
        }

        public NdkInfo ActiveNDK
        {
            get { return PackageViewModel.Instance.ActiveNDK; }
            set { PackageViewModel.Instance.ActiveNDK = value; }
        }

        public IEventDispatcher Dispatcher
        {
            get;
            set;
        }

        public void ReloadAndActivate(NdkInfo ndk)
        {
            PackageViewModel.Instance.ResetNDKs();
            ActiveNDK = ndk;
        }

        public ApiLevelActionType GetActionForNDK(ApiInfo definition)
        {
            object argument;

            return GetActionForNDK(definition, out argument);
        }

        public ApiLevelActionType GetActionForNDK(ApiInfo definition, out object argument)
        {
            argument = null;

            // nothing should be displayed for not defined NDK:
            if (definition == null)
                return ApiLevelActionType.Hide;

            // check, if it exists on disk:
            bool isInstalled = PackageViewModel.Instance.IndexOfInstalled(definition.Version) >= 0;
            var ndkInfo = definition as NdkInfo;

            if (ndkInfo != null)
            {
                // is it NDK owned by the plugin itself?
                if (isInstalled && ndkInfo.TargetPath != null && ndkInfo.TargetPath.StartsWith(RunnerDefaults.NdkDirectory))
                {
                    return ApiLevelActionType.Uninstall;
                }

                // is it a custom definition added?
                if (ndkInfo.FilePath != null && ndkInfo.FilePath.StartsWith(RunnerDefaults.PluginInstallationConfigDirectory))
                {
                    return ApiLevelActionType.Forget;
                }
            }

            // is it a PlayBook NDK?
            if (definition.Version <= LastPlayBookVersion)
            {
                if (isInstalled)
                    return ApiLevelActionType.Nothing;

                argument = "http://developer.blackberry.com/playbook/native/download/";
                return ApiLevelActionType.InstallManually;
            }

            // by default, check, if it can be installed, or it's not owned by the pluign:
            return isInstalled ? ApiLevelActionType.Nothing : ApiLevelActionType.Install;
        }

        public bool LoadNDKs(bool reload)
        {
            // is it still loading?
            if (_ndkLoadRunner != null)
                return false;

            // did we loaded it before
            if (!reload && RemoteNDKs.Length > 0)
            {
                if (NdkListLoaded != null)
                    NdkListLoaded(this, EventArgs.Empty);
                return false;
            }

            _ndkLoadRunner = new ApiLevelListLoadRunner(RunnerDefaults.NdkDirectory, ApiLevelListTypes.Full);
            _ndkLoadRunner.Dispatcher = Dispatcher;
            _ndkLoadRunner.Finished += NdkLoadRunnerOnFinished;
            _ndkLoadRunner.ExecuteAsync();
            return true;
        }

        private void NdkLoadRunnerOnFinished(object sender, ToolRunnerEventArgs e)
        {
            PackageViewModel.Instance.RemoteNDKs = _ndkLoadRunner.ApiLevels;
            _ndkLoadRunner.Finished -= NdkLoadRunnerOnFinished;
            _ndkLoadRunner = null;

            if (NdkListLoaded != null)
            {
                NdkListLoaded(this, EventArgs.Empty);
            }
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_ndkLoadRunner != null)
            {
                _ndkLoadRunner.Dispose();
                _ndkLoadRunner = null;
            }
        }

        #endregion

        public bool CheckIfNdkInstalled(ApiInfo info)
        {
            return info != null && PackageViewModel.Instance.IndexOfInstalled(info.Version) >= 0;
        }

        public bool IsProcessingNDK(ApiInfo info)
        {
            return info != null && PackageViewModel.Instance.UpdateManager.IsProcessingNDK(info.Version);
        }

        /// <summary>
        /// Requests to download NDK.
        /// </summary>
        public void RequestNdk(ApiInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            PackageViewModel.Instance.UpdateManager.Request(UpdateActions.Install, UpdateActionTargets.NDK, info.Version);
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
