using System;
using System.Windows.Media;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProcessListViewItem : BaseViewItem
    {
        public ProcessListViewItem(TargetServiceSysInfo service)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            Service = service;
            AddExpandablePlaceholder();
        }

        #region Properties

        public override string Name
        {
            get { return "Processes"; }
        }

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        public TargetServiceSysInfo Service
        {
            get;
            private set;
        }

        #endregion

        protected override void LoadItems()
        {
            BaseViewItem[] items;

            try
            {
                var processes = Service.LoadProcesses();

                items = new BaseViewItem[processes.Length];
                for (int i = 0; i < processes.Length; i++)
                {
                    items[i] = new ProcessViewItem(processes[i]);
                }
            }
            catch (Exception ex)
            {
                items = new BaseViewItem[] { new MessageViewItem(ex) };
            }

            OnItemsLoaded(items);
        }
    }
}
