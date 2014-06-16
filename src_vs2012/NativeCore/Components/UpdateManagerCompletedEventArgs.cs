using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Components
{
    /// <summary>
    /// Arguments passed along with Completed event of UpdateManager.
    /// </summary>
    public sealed class UpdateManagerCompletedEventArgs : EventArgs
    {
        internal UpdateManagerCompletedEventArgs(ApiLevelTarget target)
        {
            Target = target;
        }

        #region Properties

        public ApiLevelTarget Target
        {
            get;
            private set;
        }

        #endregion
    }
}
