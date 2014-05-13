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

        private NdkInfo[] _installedNDKs;

        public PackageViewModel()
        {
        }

        #region Properties

        public NdkInfo[] InstalledNDKs
        {
            get
            {
                if (_installedNDKs == null)
                {
                    // load info about NDKs from specified locations:
                    _installedNDKs = NdkInfo.Load(RunnerDefaults.InstallationConfigDirectory, RunnerDefaults.SupplementaryInstallationConfigDirectory);
                }

                return _installedNDKs;
            }
        }

    #endregion
    }
}
