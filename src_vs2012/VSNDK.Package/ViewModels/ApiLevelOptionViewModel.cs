using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class ApiLevelOptionViewModel
    {
        public NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
        }

        public NdkInfo ActiveNDK
        {
            get { return PackageViewModel.Instance.ActiveNDK; }
            set { PackageViewModel.Instance.ActiveNDK = value; }
        }

        public void ReloadAndActivate(NdkInfo ndk)
        {
            PackageViewModel.Instance.ResetNDKs();
            ActiveNDK = ndk;
        }
    }
}
