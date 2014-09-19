using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackBerry.NativeCore.Model;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ViewModels.TargetNavigator;

namespace BlackBerry.Package.ViewModels
{
    /// <summary>
    /// View model class for navigating over file system of the target.
    /// </summary>
    internal sealed class TargetNavigatorViewModel : INotifyPropertyChanged
    {
        #region Internal Classes

        sealed class NavigationArgs
        {
            private int _level;

            public NavigationArgs(string path)
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentNullException("path");

                OriginalPath = path;
                Segments = path.Split('/');

                // move to next segment:
                Path = path;
                _level = 0;
            }

            #region Properties

            public TargetViewItem Target
            {
                get;
                set;
            }

            public string Path
            {
                get;
                private set;
            }

            public string OriginalPath
            {
                get;
                private set;
            }

            public string[] Segments
            {
                get;
                private set;
            }

            public string Name
            {
                get { return _level >= Segments.Length ? null : Segments[_level]; }
            }

            #endregion

            public bool GoDownThePath()
            {
                return GoDownThePath(1);
            }

            public bool GoDownThePath(int segments)
            {
                _level += segments;

                // remove specified number of segments from Path's beginning:
                if (!string.IsNullOrEmpty(Path) && segments > 0)
                {
                    int at = 0;
                    for (int i = 0; i < segments && at >= 0; i++)
                    {
                        at = Path.IndexOf('/', at + 1);
                    }

                    Path = at > 0 ? Path.Substring(at) : string.Empty;
                }

                return _level < Segments.Length;
            }
        }

        #endregion

        private readonly PackageViewModel _packageViewModel;
        private readonly Dictionary<string, ImageSource> _iconCache;
        private BaseViewItem _selectedItem;
        private BaseViewItem _selectedItemListSource;

        public TargetNavigatorViewModel(PackageViewModel packageViewModel)
        {
            if (packageViewModel == null)
                throw new ArgumentNullException("packageViewModel");

            _packageViewModel = packageViewModel;
            _iconCache = new Dictionary<string, ImageSource>();

            // initialize target devices:
            Targets = new ObservableCollection<TargetViewItem>();
            foreach (var target in _packageViewModel.TargetDevices)
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
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    NotifyPropertyChanged("SelectedItem");

                    var newSelectedItemSource = value != null ? (value.IsEnumerable ? value : value.Parent) : null;
                    if (newSelectedItemSource != _selectedItemListSource)
                    {
                        _selectedItemListSource = newSelectedItemSource;
                        NotifyPropertyChanged("SelectedItemListSource");
                    }
                }
            }
        }

        public BaseViewItem SelectedItemListSource
        {
            get { return _selectedItemListSource; }
            set
            {
                if (value != null)
                {
                    if (!value.IsEnumerable)
                    {
                        var newSelected = value is FileToParentViewItem ? value.Parent : value;
                        if (_selectedItem != newSelected && newSelected != null)
                        {
                            newSelected.IsSelected = true;
                        }
                    }
                }

                // since the 'value' comes from the list-view (items listing panel on the right)
                // as a source to that panel we need still to serve it's parent:
                if (value != null)
                {
                    value = value.Parent;
                }

                if (_selectedItemListSource != value)
                {
                    _selectedItemListSource = value;
                    NotifyPropertyChanged("SelectedItemListSource");
                }
            }
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

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// Updates info about specified device.
        /// </summary>
        public void Update(DeviceDefinition oldDevice, DeviceDefinition newDevice)
        {
            if (oldDevice == null)
                throw new ArgumentNullException("oldDevice");
            if (newDevice == null)
                throw new ArgumentNullException("newDevice");

            _packageViewModel.Update(oldDevice, newDevice);
        }

        /// <summary>
        /// Navigates over the whole tree of items and selects the one specified.
        /// </summary>
        public bool NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            path = path.Trim();
            if (SelectedItem != null)
            {
                if (string.Compare(path, SelectedItem.NavigationPath, StringComparison.CurrentCulture) == 0)
                    return false;
            }

            var navArgs = new NavigationArgs(path);
            TargetViewItem target = Targets.Count > 0 ? Targets[0] : null;

            // does the path points to the specified target?
            if (navArgs.Name.EndsWith(":"))
            {
                target = FindTarget(navArgs.Name);
            }

            if (target == null)
            {
                // no way to find anything if no targets defined or non-matching name...
                return false;
            }

            if (navArgs.GoDownThePath())
            {
                navArgs.Target = target;
                InternalStartNavigateTo(navArgs);
                return true;
            }

            // there was only target specified in the path:
            target.IsSelected = true;
            target.IsExpanded = true;
            return true;
        }

        /// <summary>
        /// Find target with maching navigation name (the one used in construction of NavigationPath for each child item).
        /// </summary>
        private TargetViewItem FindTarget(string navigationName)
        {
            foreach (var target in Targets)
            {
                if (string.Compare(target.NavigationPath, navigationName, StringComparison.CurrentCultureIgnoreCase) == 0)
                    return target;
            }

            return null;
        }

        private void InternalStartNavigateTo(NavigationArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            if (e.Target == null)
                throw new ArgumentOutOfRangeException("e");

            NavigateTo(e.Target, e);
        }

        private void NavigateTo(BaseViewItem item, NavigationArgs e)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            item.ItemsAvailable += ViewItemsAvailable;
            item.Tag = e;
            item.IsSelected = true;
            item.IsExpanded = true;
            item.Refresh(); // continue looking down...
        }

        private void ViewItemsAvailable(object sender, EventArgs e)
        {
            var item = (BaseViewItem) sender;
            var navArgs = (NavigationArgs) item.Tag;

            // ignore loading progress notifications...
            if (item.Children.Count == 1 && item.Children[0] is ProgressViewItem)
                return;

            item.ItemsAvailable -= ViewItemsAvailable;
            item.Tag = null;
            if (navArgs == null)
            {
                return;
            }

            // find navigation sub-item:
            int matchingSegments;
            var nav = item.FindByNavigation(navArgs.Path, navArgs.Name, out matchingSegments);

            // if we should go down further:
            if (nav != null)
            {
                if (navArgs.GoDownThePath(matchingSegments))
                {
                    NavigateTo(nav, navArgs);
                }
                else
                {
                    nav.IsSelected = true;
                    nav.IsActivated = true;
                }
            }
            else
            {
                item.IsSelected = true;
                item.IsActivated = true;
            }
        }
    }
}
