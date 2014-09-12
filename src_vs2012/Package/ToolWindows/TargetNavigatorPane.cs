using System.Runtime.InteropServices;
using BlackBerry.Package.ToolWindows.ViewModel;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.ToolWindows
{
    [Guid("854ea6df-e8b0-4da7-bbd5-17e9d7753426")]
    public sealed class TargetNavigatorPane : ToolWindowPane
    {
        public TargetNavigatorPane()
        {
            Caption = "Target Navigator";

            var control = new TargetFileSystemNavigatorControl();
            control.DataContext = new TargetNavigatorViewModel();
            Content = control;
        }
    }
}
