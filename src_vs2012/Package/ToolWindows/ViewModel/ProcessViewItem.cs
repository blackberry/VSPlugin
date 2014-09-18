using System;
using System.Windows.Forms;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProcessViewItem : BaseViewItem
    {
        private readonly SystemInfoProcess _process;
        private readonly TargetServiceControl _service;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ProcessViewItem(TargetNavigatorViewModel viewModel, TargetServiceControl service, SystemInfoProcess process)
            : base(viewModel)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (process == null)
                throw new ArgumentNullException("process");

            ContextMenuName = "ContextForProcess";
            _service = service;
            _process = process;
            ImageSource = ViewModel.GetIconForProcess();
        }

        #region Properties

        public override string Name
        {
            get { return _process.ShortExecutablePath; }
        }

        public string ID
        {
            get { return "0x" + _process.ID.ToString("X8"); }
        }

        public string ParentID
        {
            get { return "0x" + _process.ParentID.ToString("X8"); }
        }

        public string ExecutablePath
        {
            get { return _process.ExecutablePath; }
        }

        public override bool IsEnumerable
        {
            get { return false; }
        }

        public bool CanTerminate
        {
            get { return _process.ExecutablePath != "usr/sbin/qconn"; }
        }

        #endregion

        protected override void ItemsCompleted(object state)
        {
            UpdateContent(this);
        }

        public bool Terminate()
        {
            if (CanTerminate)
            {
                if (MessageBoxHelper.Show(Name, "Do you really want to terminate this process?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _service.Terminate(_process);

                    // and force the parent node to reload the items, to make the currently executed one to disappear:
                    if (Parent != null)
                    {
                        Parent.ForceReload();
                    }
                }

                return true;
            }

            return false;
        }
    }
}
