using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.NativeCore.QConn.Visitors;
using BlackBerry.Package.Dialogs;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.ViewModels.TargetNavigator
{
    internal class FileViewItem : BaseViewItem
    {
        private const string NoAccess = "N/A";
        public const ulong MaxFileSize = 4 * 1024 * 1024;

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
                ContextMenuName = "ContextForFolder";
                ImageSource = ViewModel.GetIconForFolder(false);
                AddExpandablePlaceholder();
            }
            else
            {
                ContextMenuName = "ContextForFile";
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
                _path = _service.Stat(_path.Path);
                OnItemsLoaded(new BaseViewItem[0], null, null);
            }
        }

        protected override void ItemsCompleted(object state)
        {
            // since we might did reloaded the file/folder info:
            NotifyPropertyChanged("CreationTime");
            NotifyPropertyChanged("Size");
            NotifyPropertyChanged("Owner");
            NotifyPropertyChanged("Group");
            NotifyPropertyChanged("Permissions");
        }

        protected override void Selected()
        {
            // if it's not so big file and we have access to open it:
            if (_path.IsFile && !_path.NoAccess && _path.Size < MaxFileSize)
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

        protected override string PresentNavigationPath()
        {
            var root = GetRoot();
            return root != null ? root + _path.Path : _path.Path;
        }

        public override bool MatchesNavigationSegment(string path, string segment, out int matchingSegments)
        {
            if (_path != null && string.Compare(segment, _path.Name, StringComparison.CurrentCulture) == 0)
            {
                matchingSegments = 1;
                return true;
            }

            matchingSegments = 0;
            return false;
        }

        public void CreateFolder()
        {
            if (CreateNewFolder(_service, _path))
            {
                ForceReload();
            }
        }

        /// <summary>
        /// Creates new folder at specified location. It will ask via UI about the name etc.
        /// </summary>
        public static bool CreateNewFolder(TargetServiceFile service, TargetFile location)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (location == null)
                throw new ArgumentNullException("location");

            // do we have access to this folder?
            if (!location.NoAccess && location.IsDirectory)
            {
                var form = new FolderForm("New Folder");
                form.FolderLocation = location.Path;

                if (form.ShowDialog() == DialogResult.OK)
                {
                    string message = null;

                    try
                    {
                        var folder = service.CreateFolder(location, form.FolderName);

                        if (folder != null)
                        {
                            // ok, did it!
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        message = ex.Message;
                    }

                    MessageBoxHelper.Show(message, "Unable to create folder \"" + form.FolderName + "\" at specified location.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return false;
            }

            MessageBoxHelper.Show(null, "Missing write permissions at: " + location.Path, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }
}
