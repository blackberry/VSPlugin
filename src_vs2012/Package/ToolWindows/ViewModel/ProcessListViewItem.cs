using System;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProcessListViewItem : BaseViewItem
    {
        public ProcessListViewItem(TargetNavigatorViewModel viewModel, TargetServiceSysInfo service)
            : base(viewModel)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            Service = service;
            ImageSource = ViewModel.GetIconForFolder(false);
            AddExpandablePlaceholder();
        }

        #region Properties

        public override string Name
        {
            get { return "Processes"; }
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
                    items[i] = new ProcessViewItem(ViewModel, processes[i]);
                }
            }
            catch (Exception ex)
            {
                items = new BaseViewItem[] { new MessageViewItem(ViewModel, ex) };
            }

            AdoptItems(items);
            OnItemsLoaded(items, items, null);
        }
    }
}
