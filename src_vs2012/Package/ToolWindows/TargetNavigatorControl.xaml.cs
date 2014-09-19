using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlackBerry.NativeCore.Components;
using BlackBerry.Package.ViewModels;
using BlackBerry.Package.ViewModels.TargetNavigator;

namespace BlackBerry.Package.ToolWindows
{
    /// <summary>
    /// Interaction logic for TargetFileSystemNavigatorControl.xaml
    /// </summary>
    public partial class TargetFileSystemNavigatorControl : UserControl
    {
        public TargetFileSystemNavigatorControl()
        {
            InitializeComponent();
        }

        #region Properties

        internal TargetNavigatorViewModel ViewModel
        {
            get { return (TargetNavigatorViewModel) DataContext; }
            set { DataContext = value; }
        }

        #endregion

        private void ListPreview_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            var item = listView != null ? listView.SelectedItem as BaseViewItem : null;

            if (item != null)
            {
                item.ExecuteDefaultAction();
            }
        }

        private void NavigateToItem_OnClick(object sender, RoutedEventArgs e)
        {
            ViewModel.NavigateTo(NavigationPath.Text);
        }

        private void TerminateProcess_OnClick(object sender, RoutedEventArgs e)
        {
            var process = GetViewItem(sender) as ProcessViewItem;

            if (process != null)
            {
                process.Terminate();
            }
        }

        private void TreeView_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var item = FindVisualUpward(e.OriginalSource as DependencyObject);

            // select item beneath the mouse, to show respective ContextMenu
            // (also that's why event is not handled here!)
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var treeView = sender as TreeView;
            var listView = sender as ListView;

            if (treeView != null)
            {
                treeView.ContextMenu = GetContextMenu(ViewModel.SelectedItem);
            }

            if (listView != null)
            {
                var item = listView.SelectedItem as BaseViewItem;
                listView.ContextMenu = GetContextMenu(item ?? ViewModel.SelectedItem);
            }
        }

        private static TreeViewItem FindVisualUpward(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private ContextMenu GetContextMenu(BaseViewItem viewItem)
        {
            if (viewItem != null && !string.IsNullOrEmpty(viewItem.ContextMenuName) && Resources.Contains(viewItem.ContextMenuName))
            {
                var contextMenu = Resources[viewItem.ContextMenuName] as ContextMenu;
                if (contextMenu != null)
                {
                    contextMenu.DataContext = viewItem;
                }
                return contextMenu;
            }

            return null;
        }

        private BaseViewItem GetViewItem(object sender)
        {
            var element = (sender as FrameworkElement);
            return element != null ? element.DataContext as BaseViewItem : null;
            //return ViewModel.SelectedItem;
        }

        private void EditTarget_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedTarget = GetViewItem(sender) as TargetViewItem;

            if (selectedTarget != null)
            {
                selectedTarget.EditProperties();
            }
        }

        private void DisconnectTarget_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedTarget = GetViewItem(sender) as TargetViewItem;

            if (selectedTarget != null)
            {
                // force the specified device to disconnect:
                Targets.Disconnect(selectedTarget.Device);
            }
        }

        private void RefreshItem_OnClick(object sender, RoutedEventArgs e)
        {
            var item = GetViewItem(sender);

            if (item != null)
            {
                item.ForceReload();
            }
        }

        private void NewFolder_OnClick(object sender, RoutedEventArgs e)
        {
            var item = ViewModel.SelectedItem as FileViewItem;

            if (item != null)
            {
                item.CreateFolder();
            }
        }
    }
}
