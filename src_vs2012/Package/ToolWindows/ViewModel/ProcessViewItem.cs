using System;
using System.Windows.Media;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProcessViewItem : BaseViewItem
    {
        private readonly SystemInfoProcess _process;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProcessViewItem(SystemInfoProcess process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            _process = process;
        }

        #region Properties

        public override string Name
        {
            get { return _process.ExecutablePath; }
        }

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        #endregion
    }
}
