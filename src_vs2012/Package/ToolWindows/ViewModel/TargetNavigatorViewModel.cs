using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    /// <summary>
    /// View model class for navigating over file system of the target.
    /// </summary>
    public sealed class TargetNavigatorViewModel
    {
        private readonly Dictionary<string, ImageSource> _iconCache;

        public TargetNavigatorViewModel()
        {
            _iconCache = new Dictionary<string, ImageSource>();

            // initialize target devices:
            Targets = new ObservableCollection<TargetViewItem>();
            foreach (var target in PackageViewModel.Instance.TargetDevices)
            {
                Targets.Add(new TargetViewItem(this, target));
            }
        }

        #region Properties

        public ObservableCollection<TargetViewItem> Targets
        {
            get;
            private set;
        }

        public BaseViewItem SelectedItem
        {
            get;
            set;
        }

        #endregion

        public ImageSource GetIconForTarget(bool connected)
        {
            return GetIcon(connected ? "target.png" : "target_disconnected.png");
        }

        public ImageSource GetIconForFolder(bool opened)
        {
            return GetIcon(opened ? "folder_opened.png" : "folder_closed.png");
        }

        public ImageSource GetIconForFile(string name)
        {
            string extension = string.IsNullOrEmpty(name) ? ".txt" : Path.GetExtension(name);
            if (string.IsNullOrEmpty(extension))
                extension = ".txt";

            // extensions must have start with a dot!
            if (extension[0] != '.')
                throw new ArgumentOutOfRangeException("name");

            return GetIcon(extension);
        }

        public ImageSource GetIconForProcess()
        {
            return GetIcon("process.png");
        }

        public ImageSource GetIconForThread()
        {
            return GetIcon("thread.png");
        }

        private ImageSource GetIcon(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            ImageSource result;

            // check if cached:
            if (_iconCache.TryGetValue(name, out result))
                return result;

            // check if looking for a matching image for any file:
            if (name[0] == '.')
            {
                result = IconHelper.GetIcon(name, true, false, false); // load system-shell image for specified extension
            }
            else
            {
                // or dedicated item image:
                result = new BitmapImage(new Uri("pack://application:,,,/BlackBerry.Package;component/Resources/Navigator/" + name));
                result.Freeze();
            }

            // cache it and done:
            _iconCache[name] = result;
            return result;
        }
    }
}
