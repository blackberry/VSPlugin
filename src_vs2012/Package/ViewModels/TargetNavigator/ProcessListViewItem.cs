using System;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ViewModels.TargetNavigator
{
    internal sealed class ProcessListViewItem : BaseViewItem
    {
        public ProcessListViewItem(TargetNavigatorViewModel viewModel, DeviceDefinition device, TargetServiceSysInfo service, TargetServiceControl control)
            : base(viewModel)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (service == null)
                throw new ArgumentNullException("service");
            if (control == null)
                throw new ArgumentNullException("control");

            ContextMenuName = "ContextRefresh";
            Device = device;
            Service = service;
            Control = control;
            ImageSource = ViewModel.GetIconForFolder(false);
            AddExpandablePlaceholder();
        }

        #region Properties

        public override string Name
        {
            get { return "Processes"; }
        }

        public DeviceDefinition Device
        {
            get;
            private set;
        }

        public TargetServiceSysInfo Service
        {
            get;
            private set;
        }

        public TargetServiceControl Control
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
                    string arguments;
                    string[] environmentVariables;

                    try
                    {
                        arguments = Service.LoadArguments(processes[i]);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to load arguments for process: {0} ({1})", processes[i].Name, processes[i].ID);
                        arguments = "Error: Unable to load";
                    }

                    try
                    {
                        environmentVariables = Service.LoadEnvironmentVariables(processes[i]);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Unable to load environment variables for process: {0} ({1})", processes[i].Name, processes[i].ID);
                        environmentVariables = new[] { "Error: Unable to load" };
                    }

                    items[i] = new ProcessViewItem(ViewModel, Device, Control, processes[i], arguments, environmentVariables);
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
