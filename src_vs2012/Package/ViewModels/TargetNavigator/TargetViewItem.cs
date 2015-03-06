using System;
using System.Text;
using System.Windows.Forms;
using BlackBerry.NativeCore.Components;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Dialogs;

namespace BlackBerry.Package.ViewModels.TargetNavigator
{
    internal sealed class TargetViewItem : BaseViewItem
    {
        public TargetViewItem(TargetNavigatorViewModel viewModel, DeviceDefinition device)
            : base(viewModel)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            ContextMenuName = "ContextForTarget";
            Device = device;
            ImageSource = ViewModel.GetIconForTarget(Targets.IsConnected(device));
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

        protected override bool CanAutoExpand
        {
            get { return true; }
        }

        public bool CanConnect
        {
            get { return !Targets.IsConnected(Device) && !Targets.IsConnecting(Device); }
        }

        public bool CanDisconnect
        {
            get { return Targets.IsConnected(Device); }
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
                            new ProcessListViewItem(ViewModel, e.Device, e.Client.SysInfoService, e.Client.ControlService),
                            new FileSystemViewItem(ViewModel, "Sandboxes", e.Client.FileService, "/accounts/1000/appdata", file => !file.NoAccess),
                            new FileSystemViewItem(ViewModel, "Shared", e.Client.FileService, "/accounts/1000/shared", null),
                            new FileSystemViewItem(ViewModel, "Developer", e.Client.FileService, "/accounts/devuser", null), 
                            new FileSystemViewItem(ViewModel, "System", e.Client.FileService, null, null)
                        };
                    break;
                case TargetStatus.Disconnected:
                    items = new BaseViewItem[]
                        {
                            new MessageViewItem(ViewModel, e.Message)
                        };
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

            AdoptItems(items);
            OnItemsLoaded(items, items, e);

            NotifyPropertyChanged("CanConnect");
            NotifyPropertyChanged("CanDisconnect");
        }

        protected override void ItemsCompleted(object state)
        {
            var e = state as TargetConnectionEventArgs;

            if (e != null)
            {
                ImageSource = ViewModel.GetIconForTarget(e.Status == TargetStatus.Connected);

                // force the child-items to be reloaded on next refresh:
                if (e.Status == TargetStatus.Disconnected)
                {
                    InvalidateItems();
                }
            }
        }

        protected override string PresentNavigationPath()
        {
            return Name + ':';
        }

        protected override void PresentName(StringBuilder buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            buffer.Append(Name).Append(':');
        }

        #endregion

        public void EditProperties()
        {
            // show form initialized with current device's data:
            var form = new DeviceForm("Edit Target Device");
            form.FromDevice(Device);

            if (form.ShowDialog() == DialogResult.OK)
            {
                ViewModel.Update(Device, form.ToDevice());
            }
        }
    }
}
