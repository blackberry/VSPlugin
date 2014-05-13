using RIM.VSNDK_Package.Model;

namespace RIM.VSNDK_Package.ViewModels
{
    internal sealed class ApiLevelViewModel
    {
        public NdkInfo[] InstalledNDKs
        {
            get { return PackageViewModel.Instance.InstalledNDKs; }
        }
    }
}
