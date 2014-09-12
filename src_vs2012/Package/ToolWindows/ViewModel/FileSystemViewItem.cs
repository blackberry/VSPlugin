using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class FileSystemViewItem : BaseViewItem
    {
        private readonly string _name;
        private readonly TargetServiceFile _service;
        private readonly Predicate<TargetFile> _filter;

        public FileSystemViewItem(TargetNavigatorViewModel viewModel, string name, TargetServiceFile service, string path, Predicate<TargetFile> filter)
            : base(viewModel)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (service == null)
                throw new ArgumentNullException("service");

            _name = name;
            _service = service;
            Path = string.IsNullOrEmpty(path) ? "/" : path;
            _filter = filter;

            ImageSource = ViewModel.GetIconForFolder(false);
            AddExpandablePlaceholder();
        }

        #region Properties

        public override string Name
        {
            get { return _name; }
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
                    items = new BaseViewItem[] { new MessageViewItem(ViewModel, "Invalid path, reload the parent folder to check if not deleted") };
                }
                else
                {
                    items = ListItems(ViewModel, _service, path, _filter);
                }
            }
            catch (Exception ex)
            {
                items = new BaseViewItem[] { new MessageViewItem(ViewModel, ex) };
            }

            OnItemsLoaded(items);
        }

        /// <summary>
        /// Method to list synchronously content of the folder.
        /// </summary>
        internal static BaseViewItem[] ListItems(TargetNavigatorViewModel viewModel, TargetServiceFile service, TargetFile path, Predicate<TargetFile> filter)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            if (service == null)
                throw new ArgumentNullException("service");
            if (path == null)
                throw new ArgumentNullException("path");

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
                            items[i] = new FileViewItem(viewModel, service, files[i], filter);
                        }
                    }
                    else
                    {
                        var filtered = new List<BaseViewItem>();

                        foreach (var file in files)
                        {
                            if (filter(file))
                            {
                                filtered.Add(new FileViewItem(viewModel, service, file, filter));
                            }
                        }
                        items = filtered.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    items = new BaseViewItem[] { new MessageViewItem(viewModel, ex) };
                }
            }

            return items;
        }
    }
}
