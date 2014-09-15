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

        private void ListPreview_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listView = sender as ListView;
            var item = listView != null ? listView.SelectedItem as BaseViewItem : null;

            if (item != null)
            {
                item.ExecuteDefaultAction();
            }
        }
    }
}
