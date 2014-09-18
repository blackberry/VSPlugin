using System;
using System.Collections.Generic;
using System.Threading;
using BlackBerry.NativeCore.Diagnostics;
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

            ContextMenuName = "ContextForFileSystem";
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
            FileViewItem[] content = null;

            try
            {
                var path = _service.Stat(Path);
                if (path == null)
                {
                    items = new BaseViewItem[] { new MessageViewItem(ViewModel, "Invalid path, reload the parent folder to check if not deleted") };
                }
                else
                {
                    items = ListItems(ViewModel, _service, path, _filter, true, out content);
                }
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex);

                items = new BaseViewItem[] { new MessageViewItem(ViewModel, ex) };
            }

            AdoptItems(content);
            OnItemsLoaded(items, content, null);
        }

        /// <summary>
        /// Method to list synchronously content of the folder.
        /// </summary>
        internal static BaseViewItem[] ListItems(TargetNavigatorViewModel viewModel, TargetServiceFile service, TargetFile path, Predicate<TargetFile> filter, bool canGoUp, out FileViewItem[] contentFilesAndFolders)
        {
            if (viewModel == null)
                throw new ArgumentNullException("viewModel");
            if (service == null)
                throw new ArgumentNullException("service");
            if (path == null)
                throw new ArgumentNullException("path");

            BaseViewItem[] items;
            var goUp = canGoUp ? new FileToParentViewItem(viewModel, service) : null;

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

                        if (canGoUp)
                        {
                            // add '..':
                            contentFilesAndFolders = new FileViewItem[items.Length + 1];
                            contentFilesAndFolders[0] = goUp;

                            // and all other files/folders:
                            for (int i = 0; i < files.Length; i++)
                            {
                                items[i] = contentFilesAndFolders[i + 1] = new FileViewItem(viewModel, service, files[i], null);
                            }
                        }
                        else
                        {
                            contentFilesAndFolders = new FileViewItem[items.Length];

                            // only all other files/folders:
                            for (int i = 0; i < files.Length; i++)
                            {
                                items[i] = contentFilesAndFolders[i] = new FileViewItem(viewModel, service, files[i], null);
                            }
                        }
                    }
                    else
                    {
                        var filtered = new List<BaseViewItem>();
                        var filteredFiles = new List<FileViewItem>();

                        // add '..' if allowed:
                        if (canGoUp)
                        {
                            filteredFiles.Add(goUp);
                        }

                        // enumerate all files/folders and return all matching the filter:
                        foreach (var file in files)
                        {
                            if (filter(file))
                            {
                                var newItem = new FileViewItem(viewModel, service, file, filter);
                                filtered.Add(newItem);
                                filteredFiles.Add(newItem);
                            }
                        }

                        // prepare results:
                        items = filtered.ToArray();
                        contentFilesAndFolders = filteredFiles.ToArray();
                    }

                }
                catch (Exception ex)
                {
                    items = new BaseViewItem[] { new MessageViewItem(viewModel, ex) };
                    contentFilesAndFolders = canGoUp ? new FileViewItem[] { goUp } : new FileViewItem[0]; // '..' to go up or nothing inside...
                }
            }

            return items;
        }

        protected override string PresentNavigationPath()
        {
            var root = GetRoot();
            return root != null ? root + Path : Path;
        }

        public override bool MatchesNavigationSegment(string path, string segment, out int matchingSegments)
        {
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Path, StringComparison.CurrentCulture))
            {
                matchingSegments = Count(Path, '/') + (Path[0] == '/' ? 0 : 1);
                return true;
            }

            matchingSegments = 0;
            return false;
        }

        /// <summary>
        /// Counts the occurrences of specified char.
        /// </summary>
        private static int Count(string text, char c)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int result = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == c)
                    result++;
            }

            return result;
        }
    }
}
