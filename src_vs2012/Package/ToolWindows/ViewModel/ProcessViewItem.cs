using System;
using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProcessViewItem : BaseViewItem
    {
        private readonly SystemInfoProcess _process;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProcessViewItem(TargetNavigatorViewModel viewModel, SystemInfoProcess process)
            : base(viewModel)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            _process = process;
            ImageSource = ViewModel.GetIconForProcess();
        }

        #region Properties

        public override string Name
        {
            get { return _process.ExecutablePath; }
        }

        #endregion
    }
}
