using System.Collections.ObjectModel;
using BlackBerry.Package.ViewModels;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    /// <summary>
    /// View model class for navigating over file system of the target.
    /// </summary>
    public sealed class TargetNavigatorViewModel
    {
        public TargetNavigatorViewModel()
        {
            // initialize target devices:
            Targets = new ObservableCollection<TargetViewItem>();
            foreach (var target in PackageViewModel.Instance.TargetDevices)
            {
                Targets.Add(new TargetViewItem(target));
            }
        }

        #region Properties

        public ObservableCollection<TargetViewItem> Targets
        {
            get;
            private set;
        }

        #endregion
    }
}
