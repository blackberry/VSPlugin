using BlackBerry.NativeCore.Model;

namespace BlackBerry.Package.ViewModels
{
    internal sealed class SigningOptionViewModel
    {
        /// <summary>
        /// Gets info about the developer.
        /// </summary>
        public DeveloperDefinition Developer
        {
            get { return PackageViewModel.Instance.Developer; }
        }
    }
}
