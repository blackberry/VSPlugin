using System;
using System.IO;
using System.IO.Packaging;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
using BlackBerry.NativeCore.QConn.Visitors;
using BlackBerry.NativeCore.Tools;
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
                        TraceLog.WriteException(ex, "Unable to create folder \"{0}\" at: \"{1}\"", form.FolderName, location);
                        message = ex.Message;
                    }

                    MessageBoxHelper.Show(message, "Unable to create folder \"" + form.FolderName + "\" at specified location.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return false;
            }

            MessageBoxHelper.Show(null, "Missing write permissions at: " + location.Path, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        public bool Delete()
        {
            if (_path.NoAccess)
            {
                MessageBoxHelper.Show(null, "Missing permissions to delete item at: " + _path.Path, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (MessageBoxHelper.Show(_path.Path, "Do you really want to delete this " + (_path.IsDirectory ? "folder" : "file") + "?",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    _service.RemoveTree(_path.Path);

                    if (Parent != null)
                    {
                        Parent.ForceReload();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return false;
        }

        public bool Rename()
        {
            if (_path.NoAccess)
            {
                MessageBoxHelper.Show(null, "Missing permissions to rename item at: " + _path.Path, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            var form = new FolderForm("Rename " + (_path.IsDirectory ? "Folder" : "File"));
            form.FolderLocation = _path.Path;
            form.FolderName = _path.Name;

            if (form.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // now, try to predict, what the developer is expecting (only rename or move it via absolute path):
                    string destinationPath = _path.CreateRenamedPath(form.FolderName);
                    _service.Move(_path.Path, destinationPath);

                    if (Parent != null)
                    {
                        Parent.ForceReload();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBoxHelper.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return false;
        }

        public bool Download(bool shouldCompress)
        {
            string destinationPath;

            if (_path.IsDirectory && !shouldCompress)
            {
                destinationPath = DialogHelper.BrowseForFolder(null, "Location for folder from taget device");
                if (string.IsNullOrEmpty(destinationPath))
                {
                    return false;
                }
            }
            else
            {
                var form = shouldCompress ? DialogHelper.SaveZipFile("Downloading from target", _path.Name) : DialogHelper.SaveAnyFile("Downloading from target", _path.Name);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    destinationPath = form.FileName;
                }
                else
                {
                    return false;
                }
            }

            // initialize download and store monitor for progress reference:
            try
            {
                var monitor = shouldCompress
                    ? _service.DownloadAsync(_path.Path, new ZipPackageVisitor(destinationPath, CompressionOption.Maximum, destinationPath))
                    : _service.DownloadAsync(_path.Path, destinationPath, destinationPath);
                ViewModel.DownloadStarted(monitor, monitor as IFileServiceVisitor);

                monitor.Dispatcher = EventDispatcher.NewForWPF();
                monitor.Failed += OnDownloadFailed;
                monitor.Completed += OnDownloadCompleted;

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Failed to initialize download from: \"{0}\" to \"{1}\"", _path.Path, destinationPath);

                MessageBoxHelper.Show(ex.Message, "Unable to initialize download operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void OnDownloadCompleted(object sender, VisitorEventArgs e)
        {
            var destinationPath = e.Tag.ToString();
            MessageBoxHelper.Show("from: " + _path.Path + "\r\n\r\nto: " + destinationPath, "Download has been completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnDownloadFailed(object sender, VisitorFailureEventArgs e)
        {
            MessageBoxHelper.Show(e.Descriptor.Path, e.UltimateMassage, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public bool Upload(bool asFile)
        {
            string sourcePath;

            if (_path.IsDirectory && !asFile)
            {
                sourcePath = DialogHelper.BrowseForFolder(null, "Folder to upload to taget device");
                if (string.IsNullOrEmpty(sourcePath))
                {
                    return false;
                }
            }
            else
            {
                var form = DialogHelper.OpenAnyFile("Uploading to target");

                if (form.ShowDialog() == DialogResult.OK)
                {
                    sourcePath = form.FileName;
                }
                else
                {
                    return false;
                }
            }

            // initialize uploading and store monitor for progress reference:
            try
            {
                var monitor = _service.UploadAsync(sourcePath, _path.Path + '/', sourcePath);
                ViewModel.UploadStarted(monitor, monitor as IFileServiceVisitor);

                monitor.Dispatcher = EventDispatcher.NewForWPF();
                monitor.Failed += OnUploadFailed;
                monitor.Completed += OnUploadCompleted;

                return true;
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Failed to initialize upload from: \"{0}\" to \"{1}\"", sourcePath, _path.Path);

                MessageBoxHelper.Show(ex.Message, "Unable to initialize upload operation", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void OnUploadCompleted(object sender, VisitorEventArgs e)
        {
            ForceReload();

            var sourcePath = e.Tag.ToString();
            MessageBoxHelper.Show("from: " + sourcePath + "\r\n\r\nto: " + _path.Path, "Upload has been completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnUploadFailed(object sender, VisitorFailureEventArgs e)
        {
            ForceReload();

            MessageBoxHelper.Show(e.Descriptor.Path, e.UltimateMassage, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
