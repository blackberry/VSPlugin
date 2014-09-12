using System;
using System.Text;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.NativeCore.QConn.Visitors;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public class FileViewItem : BaseViewItem
    {
        private readonly TargetServiceFile _service;
        private TargetFile _path;
        private readonly Predicate<TargetFile> _filter;

        public FileViewItem(TargetNavigatorViewModel viewModel, TargetServiceFile service, TargetFile path, Predicate<TargetFile> filter)
            : base(viewModel)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (path == null)
                throw new ArgumentNullException("path");

            _service = service;
            _path = path;
            _filter = filter;

            if (path.IsDirectory)
            {
                ImageSource = ViewModel.GetIconForFolder(false);
                AddExpandablePlaceholder();
            }
            else
            {
                ImageSource = ViewModel.GetIconForFile(path.Name);
            }
        }

        public override string Name
        {
            get { return _path.Name; }
        }

        protected override void LoadItems()
        {
            if (_path.IsDirectory)
            {
                BaseViewItem[] items = FileSystemViewItem.ListItems(ViewModel, _service, _path, _filter);
                OnItemsLoaded(items);
            }
            else
            {
                _path = _service.Stat(_path.Path);
                OnItemsLoaded(new BaseViewItem[0]);
            }
        }

        protected override void Selected()
        {
            // if it's not so big file and we have access to open it:
            if (_path.IsFile && !_path.NoAccess && _path.Size < 4 * 1024 * 1024)
            {
                var monitor = _service.PreviewAsync(_path.Path);
                monitor.Completed += MonitorOnCompleted;
                monitor.Failed += MonitorOnFailed;
            }
        }

        private void MonitorOnFailed(object sender, VisitorFailureEventArgs e)
        {
            var monitor = sender as BufferVisitor;

            if (monitor != null)
            {
                monitor.Completed -= MonitorOnCompleted;
                monitor.Failed -= MonitorOnFailed;

                UpdateContent(string.Concat("Unable to load content of: ", e.Descriptor, Environment.NewLine, e.UltimateMassage));
            }
        }

        private void MonitorOnCompleted(object sender, VisitorEventArgs e)
        {
            var monitor = sender as BufferVisitor;

            if (monitor != null)
            {
                monitor.Completed -= MonitorOnCompleted;
                monitor.Failed -= MonitorOnFailed;

                try
                {
                    UpdateContent(Encoding.UTF8.GetString(monitor.Data));
                }
                catch (Exception ex)
                {
                    UpdateContent(string.Concat("Unable to interpret content.", Environment.NewLine, ex.Message));
                }
            }
        }
    }
}