using System;
using System.Collections.Generic;
using System.Windows.Media;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public class FileViewItem : BaseViewItem
    {
        private readonly TargetServiceFile _service;
        private readonly TargetFile _path;
        private readonly Predicate<TargetFile> _filter;

        public FileViewItem(TargetServiceFile service, TargetFile path, Predicate<TargetFile> filter)
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
                AddExpandablePlaceholder();
            }
        }

        public override string Name
        {
            get { return _path.Name; }
        }

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        protected override void LoadItems()
        {
            BaseViewItem[] items = FileSystemViewItem.ListItems(_service, _path, _filter);
            OnItemsLoaded(items);
        }
    }
}