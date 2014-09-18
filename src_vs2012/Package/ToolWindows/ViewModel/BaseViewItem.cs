using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    /// <summary>
    /// Base class for all view model items.
    /// </summary>
    public abstract class BaseViewItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isActivated;
        private bool _isExpanded;
        private bool _isLoading;
        private bool _loadedItemsAlready;
        private ImageSource _imageSource;
        private object _content;
        private string _navigationPath;

        public event EventHandler ItemsAvailable;

        protected static readonly BaseViewItem ExpandPlaceholder = new ProgressViewItem(null, "X-X-X");

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseViewItem(TargetNavigatorViewModel viewModel)
        {
            ViewModel = viewModel;
            Children = new ObservableCollection<BaseViewItem>();
        }

        #region Properties

        public TargetNavigatorViewModel ViewModel
        {
            get;
            private set;
        }

        public BaseViewItem Parent
        {
            get;
            set;
        }

        public object Tag
        {
            get;
            set;
        }

        public abstract string Name
        {
            get;
        }

        public string NavigationPath
        {
            get
            {
                if (string.IsNullOrEmpty(_navigationPath))
                    _navigationPath = PresentNavigationPath();

                return _navigationPath;
            }
        }

        public ImageSource ImageSource
        {
            get { return _imageSource; }
            set
            {
                if (_imageSource != value)
                {
                    _imageSource = value;
                    NotifyPropertyChanged("ImageSource");
                }
            }
        }

        public ObservableCollection<BaseViewItem> Children
        {
            get;
            private set;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;

                    if (value)
                    {
                        Refresh();
                    }
                    else
                    {
                        Collapse();
                    }

                    NotifyPropertyChanged("IsExpanded");
                }
            }
        }

        /// <summary>
        /// Property indicating, if item is selected on the tree-view on the left-side.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    _isActivated = value;

                    if (value)
                    {
                        ViewModel.SelectedItem = this;
                        Selected();
                        Refresh();
                        //AutoExpand();
                    }

                    NotifyPropertyChanged("IsSelected");
                    NotifyPropertyChanged("IsActivated");
                }
            }
        }

        /// <summary>
        /// Property indicating, if item is selected on the list-view on the right-side.
        /// </summary>
        public bool IsActivated
        {
            get { return _isActivated; }
            set
            {
                if (_isActivated != value)
                {
                    _isActivated = value;
                    NotifyPropertyChanged("IsActivated");

                    if (value)
                    {
                        ViewModel.SelectedItemListSource = this; // try to evaluate
                        Selected();
                    }
                }
            }
        }

        public virtual bool IsEnumerable
        {
            get { return true; }
        }

        protected virtual bool CanAutoExpand
        {
            get { return false; }
        }

        public object Content
        {
            get { return _content; }
            set
            {
                if (_content != value)
                {
                    _content = value;
                    NotifyPropertyChanged("Content");
                }
            }
        }

        #endregion

        protected void AddExpandablePlaceholder()
        {
            Children.Add(ExpandPlaceholder);
        }

        private void AutoExpand()
        {
            if (!_loadedItemsAlready && !IsExpanded && Children.Count > 0 && CanAutoExpand)
            {
                IsExpanded = true;
            }
        }

        public void Refresh()
        {
            if (!_isLoading && !_loadedItemsAlready)
            {
                _isLoading = true;

                Children.Clear();
                var progressItem = CreateProgressPlaceholder();
                if (progressItem != null)
                {
                    Children.Add(progressItem);

                    progressItem.Parent = this;
                    UpdateContent(new BaseViewItem[] { progressItem });
                }

                // retrieve items to fill-in the list asynchronously:
                ThreadPool.QueueUserWorkItem(InternalLoadItems);
            }

            // notify, that items are already in place:
            if (_loadedItemsAlready)
            {
                NotifyItemsAvailable();
            }
        }

        public void Collapse()
        {
            InvalidateItems();
            IsExpanded = false;
        }

        public void ForceReload()
        {
            InvalidateItems();
            Refresh();
        }

        private void InternalLoadItems(object state)
        {
            try
            {
                LoadItems();
            }
            catch (Exception ex)
            {
                // to make sure, all async loads always 'complete', when exception crashed them...
                OnItemsLoaded(new BaseViewItem[] { new MessageViewItem(ViewModel, ex) });
            }
        }

        /// <summary>
        /// Method called after asynchronous items were loaded to populate them to the UI.
        /// </summary>
        protected void OnItemsLoaded(BaseViewItem[] items)
        {
            OnItemsLoaded(items, null, null);
        }

        /// <summary>
        /// Method called after asynchronous items were loaded to populate them to the UI.
        /// </summary>
        protected void OnItemsLoaded(BaseViewItem[] items, object content, object state)
        {
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                InternalRefreshItemsLoaded(items, content, state);
            }
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<BaseViewItem[], object, object>(InternalRefreshItemsLoaded), items, content, state);
            }
        }

        /// <summary>
        /// Refreshes the collection of child-items. Since it automatically fires the collection-changed events, it should be only called from the UI thread.
        /// </summary>
        private void InternalRefreshItemsLoaded(BaseViewItem[] items, object content, object state)
        {
            _loadedItemsAlready = true;
            Children.Clear();

            // did we loaded items correctly?
            if (items == null)
            {
                var error = CreateErrorPlaceholder();
                if (error != null)
                {
                    error.Parent = this;
                    Children.Add(error);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    item.Parent = this;
                    Children.Add(item);
                }
            }

            InternalUpdateContent(content);
            ItemsCompleted(state);
            _isLoading = false;

            // notify, that new items just appeared:
            NotifyItemsAvailable();
        }

        protected virtual BaseViewItem CreateProgressPlaceholder()
        {
            return new ProgressViewItem(ViewModel, "Loading...");
        }

        protected virtual BaseViewItem CreateErrorPlaceholder()
        {
            return new ProgressViewItem(ViewModel, "Failed to list items");
        }

        /// <summary>
        /// Method invoked on background thread to list children of this item.
        /// It can take as much time as needed. The progress indicator is returned by CreateProgressPlaceholder() call.
        /// </summary>
        protected virtual void LoadItems()
        {
            // by default display empty list:
            OnItemsLoaded(new BaseViewItem[0]);
        }

        /// <summary>
        /// Invoked on UI thread, when all items have been populated.
        /// </summary>
        protected virtual void ItemsCompleted(object state)
        {
        }

        protected virtual void InvalidateItems()
        {
            _loadedItemsAlready = false;
        }

        /// <summary>
        /// Invoked on UI thread, when current ViewItem has been selected.
        /// </summary>
        protected virtual void Selected()
        {
        }

        /// <summary>
        /// Updates the Content property from any thread.
        /// </summary>
        protected void UpdateContent(object content)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                InternalUpdateContent(content);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<object>(InternalUpdateContent), content);
            }
        }

        private void InternalUpdateContent(object content)
        {
            Content = content;
        }

        /// <summary>
        /// Executes default behavior, when user double-clicked this item on the UI.
        /// </summary>
        public virtual void ExecuteDefaultAction()
        {
            IsSelected = true;
            IsExpanded = true;
        }

        protected void AdoptItems(IEnumerable<BaseViewItem> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.Parent = this;
                }
            }
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies that value of specified property has changed.
        /// </summary>
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        protected void NotifyItemsAvailable()
        {
            var handler = ItemsAvailable;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #region Navigation

        /// <summary>
        /// Gets the top parent of this item.
        /// It will return null, if current object has no parent.
        /// </summary>
        protected BaseViewItem GetRoot()
        {
            var root = Parent;

            while (root.Parent != null)
            {
                root = root.Parent;
            }

            return root;
        }

        protected virtual string PresentNavigationPath()
        {
            return null;
        }

        public virtual bool MatchesNavigationSegment(string path, string segment, out int matchingSegments)
        {
            matchingSegments = 0;
            return false;
        }

        /// <summary>
        /// Finds matching child item using full path or currently active segment.
        /// Specified segment is part of the navigation path returned.
        /// </summary>
        public BaseViewItem FindByNavigation(string path, string segment, out int matchingSegments)
        {
            foreach (var item in Children)
            {
                if (item.MatchesNavigationSegment(path, segment, out matchingSegments))
                    return item;
            }

            matchingSegments = 0;
            return null;
        }

        #endregion

        #region Name Presentation

        protected virtual void PresentName(StringBuilder buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (Parent != null)
            {
                Parent.PresentName(buffer);
            }

            buffer.Append('/').Append(Name);
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            PresentName(buffer);
            return buffer.ToString();
        }

        #endregion
    }
}
