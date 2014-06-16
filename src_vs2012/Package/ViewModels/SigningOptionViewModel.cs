using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RIM.VSNDK_Package.ViewModels
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
