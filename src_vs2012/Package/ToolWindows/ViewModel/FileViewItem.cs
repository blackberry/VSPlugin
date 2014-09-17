using System;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.NativeCore.QConn.Visitors;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public class FileViewItem : BaseViewItem
    {
        private const string NoAccess = "N/A";
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

        #region Properties

        public override string Name
        {
            get { return _path == null ? NoAccess : _path.Name; }
        }

        public virtual string CreationTime
        {
            get { return _path == null || _path.NoAccess ? NoAccess : _path.CreationTime.ToString(); }
        }

        public virtual string Size
        {
            get { return _path == null || _path.NoAccess ? NoAccess : _path.Size.ToString(); }
        }

        public virtual string Owner
        {
            get { return _path == null || _path.NoAccess ? NoAccess : _path.UserID.ToString(); }
        }

        public virtual string Group
        {
            get { return _path == null || _path.NoAccess ? NoAccess : _path.GroupID.ToString(); }
        }

        public virtual string Permissions
        {
            get { return _path == null || _path.NoAccess ? NoAccess : _path.FormattedPermissions; }
        }

        public override bool IsEnumerable
        {
            get { return IsDirectory; }
        }

        public bool IsDirectory
        {
            get { return _path != null && _path.IsDirectory; }
        }

        #endregion

        protected override void LoadItems()
        {
            if (_path.IsDirectory)
            {
                FileViewItem[] content;
                BaseViewItem[] items = FileSystemViewItem.ListItems(ViewModel, _service, _path, _filter, true, out content);

                AdoptItems(content);
                OnItemsLoaded(items, content, null);
            }
            else
            {
                //_path = _service.Stat(_path.Path);
                OnItemsLoaded(new BaseViewItem[0], null, null);
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
                    if (monitor.Source != null)
                    {
                        if (IsImage(monitor.Source))
                        {
                            UpdateContentWithImage(monitor.Data);
                        }
                        else
                        {
                            if (IsText(monitor.Source))
                            {
                                UpdateContentWithText(monitor.Data);
                            }
                            else
                            {
                                UpdateContent(monitor.Data);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateContent(string.Concat("Unable to interpret content.", Environment.NewLine, ex.Message));
                }
            }
        }

        #region Preview Support

        private static bool IsImage(TargetFile file)
        {
            return file.Extension == ".png" || file.Extension == ".jpg" || file.Extension == ".bmp" || file.Extension == ".tif" || file.Extension == ".gif" || file.Extension == ".ico";
        }

        private static bool IsText(TargetFile file)
        {
            return file.Name == "log" || file.Extension == ".txt" || file.Extension == ".log" || file.Extension == ".xml" || file.Extension == ".html" || file.Extension == ".profile";
        }

        private bool UpdateContentWithImage(byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                var result = new BitmapImage();

                using (var mem = new MemoryStream(data))
                {
                    result.BeginInit();
                    result.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                    result.CacheOption = BitmapCacheOption.OnLoad;
                    result.UriSource = null;
                    result.StreamSource = mem;
                    result.EndInit();
                }
                result.Freeze();

                UpdateContent(result);
                return true;
            }

            return false;
        }

        private bool UpdateContentWithText(byte[] data)
        {
            var text = data != null && data.Length > 0 ? Encoding.UTF8.GetString(data) : string.Empty;
            UpdateContent(text);
            return true;
        }

        #endregion
    }
}
