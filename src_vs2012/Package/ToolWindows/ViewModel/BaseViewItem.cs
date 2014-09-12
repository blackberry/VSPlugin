using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private bool _isExpanded;
        private bool _isLoading;

        protected static readonly BaseViewItem ExpandPlaceholder = new ProgressViewItem("X-X-X");

        /// <summary>
        /// Default constructor.
        /// </summary>
        public BaseViewItem()
        {
            Children = new ObservableCollection<BaseViewItem>();
        }

        #region Properties

        public abstract string Name
        {
            get;
        }

        public abstract ImageSource ImageSource
        {
            get;
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

        #endregion

        protected void AddExpandablePlaceholder()
        {
            Children.Add(ExpandPlaceholder);
        }

        public void Refresh()
        {
            if (!_isLoading)
            {
                _isLoading = true;

                Children.Clear();
                var progressItem = CreateProgressPlaceholder();
                if (progressItem != null)
                {
                    Children.Add(progressItem);
                }

                // retrieve items to fill-in the list asynchronously:
                ThreadPool.QueueUserWorkItem(InternalLoadItems);
            }
        }

        public void Collapse()
        {
            IsExpanded = false;
        }

        private void InternalLoadItems(object state)
        {
            LoadItems();
        }

        /// <summary>
        /// Method called after asynchronous items were loaded to populate them to the UI.
        /// </summary>
        protected void OnItemsLoaded(BaseViewItem[] items)
        {
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                InternalRefreshItemsLoaded(items);
            }
            else
            {
                dispatcher.BeginInvoke(DispatcherPriority.Background, new Action<BaseViewItem[]>(InternalRefreshItemsLoaded), items);
            }
        }

        /// <summary>
        /// Refreshes the collection of child-items. Since it automatically fires the collection-changed events, it should be only called from the UI thread.
        /// </summary>
        private void InternalRefreshItemsLoaded(BaseViewItem[] items)
        {
            Children.Clear();

            // did we loaded items correctly?
            if (items == null)
            {
                var error = CreateErrorPlaceholder();
                if (error != null)
                {
                    Children.Add(error);
                }
            }
            else
            {
                foreach (var item in items)
                {
                    Children.Add(item);
                }
            }

            _isLoading = false;
        }

        protected virtual BaseViewItem CreateProgressPlaceholder()
        {
            return new ProgressViewItem("Loading...");
        }

        protected virtual BaseViewItem CreateErrorPlaceholder()
        {
            return new ProgressViewItem("Failed to list items");
        }

        protected virtual void LoadItems()
        {
            // by default display empty list:
            OnItemsLoaded(new BaseViewItem[0]);
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
    }
}
