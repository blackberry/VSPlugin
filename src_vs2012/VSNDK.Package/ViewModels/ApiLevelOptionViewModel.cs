using System;
using RIM.VSNDK_Package.Model;
using RIM.VSNDK_Package.Tools;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class ApiLevelOptionViewModel
    {
        private readonly static Version LastPlayBookVersion = new Version(9, 99, 0, 0);

        public NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
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

        public ApiLevelActionType GetActionForNdk(NdkInfo definition, out object argument)
        {
            argument = null;

            // nothing should be displayed for not defined NDK:
            if (definition == null)
                return ApiLevelActionType.Hide;

            // check, if it exists on disk:
            bool exists = definition.Exists();

            // is it NDK owned by the plugin itself?
            if (exists && definition.TargetPath != null && definition.TargetPath.StartsWith(RunnerDefaults.NdkDirectory))
            {
                return ApiLevelActionType.Uninstall;
            }

            // is it a custom definition added?
            if (definition.FilePath != null && definition.FilePath.StartsWith(RunnerDefaults.PluginInstallationConfigDirectory))
            {
                return ApiLevelActionType.Forget;
            }

            // is it a PlayBook NDK?
            if (definition.Version <= LastPlayBookVersion)
            {
                if (exists)
                    return ApiLevelActionType.Nothing;

                argument = "http://developer.blackberry.com/playbook/native/download/";
                return ApiLevelActionType.InstallManually;
            }

            // by default, check, if it can be installed, or it's not owned by the pluign:
            return exists ? ApiLevelActionType.Nothing : ApiLevelActionType.Install;
        }
    }
}
