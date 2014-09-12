using System;
using System.Collections.Generic;
using System.Windows.Media;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class FileSystemViewItem : BaseViewItem
    {
        private readonly string _name;
        private readonly TargetServiceFile _service;
        private readonly Predicate<TargetFile> _filter;

        public FileSystemViewItem(string name, TargetServiceFile service, string path, Predicate<TargetFile> filter)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (service == null)
                throw new ArgumentNullException("service");

            _name = name;
            _service = service;
            Path = string.IsNullOrEmpty(path) ? "/" : path;
            _filter = filter;

            AddExpandablePlaceholder();
        }

        #region Properties

        public override string Name
        {
            get { return _name; }
        }

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        public string Path
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
                var path = _service.Stat(Path);
                if (path == null)
                {
                    items = new BaseViewItem[] { new MessageViewItem("Invalid path, reload the parent folder to check if not deleted") };
                }
                else
                {
                    items = ListItems(_service, path, _filter);
                }
            }
            catch (Exception ex)
            {
                items = new BaseViewItem[] { new MessageViewItem(ex) };
            }

            OnItemsLoaded(items);
        }

        internal static BaseViewItem[] ListItems(TargetServiceFile service, TargetFile path, Predicate<TargetFile> filter)
        {
            BaseViewItem[] items;

            // we need to lock on that service here, not to allow the user to expand two nodes at the same time
            // as it might affect sync call to the service itself;
            // since this method is executed asynchronously, it's save to block...
            lock (service)
            {
                try
                {
                    var files = service.List(path);

                    if (filter == null)
                    {
                        items = new BaseViewItem[files.Length];
                        for (int i = 0; i < files.Length; i++)
                        {
                            items[i] = new FileViewItem(service, files[i], filter);
                        }
                    }
                    else
                    {
                        var filtered = new List<BaseViewItem>();

                        foreach (var file in files)
                        {
                            if (filter(file))
                            {
                                filtered.Add(new FileViewItem(service, file, filter));
                            }
                        }
                        items = filtered.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    items = new BaseViewItem[] { new MessageViewItem(ex) };
                }
            }

            return items;
        }
    }
}
