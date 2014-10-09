using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.Package.ViewModels
{
    /// <summary>
    /// Arguments passed along with the TargetsChanged event of the PackageViewModel.
    /// </summary>
    internal sealed class TargetsChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetsChangedEventArgs(DeviceDefinition[] targetDevices)
        {
            if (targetDevices == null)
                throw new ArgumentNullException("targetDevices");

            TargetDevices = targetDevices;
        }

        #region Properties

        public DeviceDefinition[] TargetDevices
        {
            get;
            private set;
        }

        #endregion
    }
}
