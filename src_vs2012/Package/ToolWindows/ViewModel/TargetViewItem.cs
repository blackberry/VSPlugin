using System;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class TargetViewItem : BaseViewItem
    {
        public TargetViewItem(TargetNavigatorViewModel viewModel, DeviceDefinition device)
            : base(viewModel)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            Device = device;
            ImageSource = ViewModel.GetIconForTarget(false);
            AddExpandablePlaceholder();
        }

        #region Properties

        public DeviceDefinition Device
        {
            get;
            private set;
        }

        public override string Name
        {
            get { return Device.ShortName; }
        }

        #endregion

        #region Overrides

        protected override BaseViewItem CreateProgressPlaceholder()
        {
            return new ProgressViewItem(ViewModel, "Connecting...");
        }

        protected override void LoadItems()
        {
            // try to connect to the device (handler will be called at least once, even if connected):
            Targets.Connect(Device, OnDeviceStatusChanged);
        }

        private void OnDeviceStatusChanged(object sender, TargetConnectionEventArgs e)
        {
            BaseViewItem[] items;

            switch(e.Status)
            {
                case TargetStatus.Connecting:
                    items = new BaseViewItem[]
                        {
                            CreateProgressPlaceholder()
                        };
                    break;
                case TargetStatus.Connected:
                    items = new BaseViewItem[]
                        {
                            new ProcessListViewItem(ViewModel, e.Client.SysInfoService),
                            new FileSystemViewItem(ViewModel, "Sandboxes", e.Client.FileService, "/accounts/1000/appdata", file => !file.NoAccess),
                            new FileSystemViewItem(ViewModel, "Shared", e.Client.FileService, "/accounts/1000/shared", null),
                            new FileSystemViewItem(ViewModel, "Developer", e.Client.FileService, "/accounts/devuser", null), 
                            new FileSystemViewItem(ViewModel, "System", e.Client.FileService, null, null)
                        };
                    break;
                case TargetStatus.Disconnected:
                    items = new BaseViewItem[0];
                    Collapse();
                    break;
                case TargetStatus.Failed:
                    items = new BaseViewItem[]
                        {
                            new MessageViewItem(ViewModel, e.Message)
                        };
                    break;
                default:
                    items = new BaseViewItem[]
                        {
                            new MessageViewItem(ViewModel, string.Concat("Unsupported device state (", e.Status, ")"))
                        };
                    break;
            }

            OnItemsLoaded(items, e);
        }

        protected override void ItemsCompleted(object state)
        {
            var e = state as TargetConnectionEventArgs;

            if (e != null)
            {
                ImageSource = ViewModel.GetIconForTarget(e.Status == TargetStatus.Connected);
            }
        }

        #endregion
    }
}
