using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using BlackBerry.Package.Helpers;
using BlackBerry.Package.ToolWindows.ViewModel;
using Button = System.Windows.Controls.Button;
using ListView = System.Windows.Controls.ListView;
using UserControl = System.Windows.Controls.UserControl;

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

        private void TerminateProcess_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var process = button != null ? button.DataContext as ProcessViewItem : null;

            if (process != null && process.CanTerminate)
            {
                if (MessageBoxHelper.Show(process.Name, "Do you really want to terminate this process?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (process.Terminate())
                    {
                        if (process.Parent != null)
                        {
                            process.Parent.ForceReload();
                        }
                    }
                }
            }
        }
    }
}
