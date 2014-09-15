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
            Content = process;
        }

        #region Properties

        public override string Name
        {
            get { return _process.ShortExecutablePath; }
        }

        public string ID
        {
            get { return "0x" + _process.ID.ToString("X4"); }
        }

        public string ParentID
        {
            get { return "0x" + _process.ParentID.ToString("X4"); }
        }

        public string ExecutablePath
        {
            get { return _process.ExecutablePath; }
        }

        #endregion
    }
}
