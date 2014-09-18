using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackBerry.Package.ToolWindows.ViewModel;

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

        public TargetNavigatorViewModel ViewModel
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
    }
}
