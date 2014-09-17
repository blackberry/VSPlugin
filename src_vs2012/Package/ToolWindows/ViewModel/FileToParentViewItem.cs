using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class FileToParentViewItem : FileViewItem
    {
        public FileToParentViewItem(TargetNavigatorViewModel viewModel, TargetServiceFile service)
            : base(viewModel, service, new TargetFile("..", "..", true), null)
        {
        }

        #region Properties

        public override string CreationTime
        {
            get { return string.Empty; }
        }

        public override string Size
        {
            get { return string.Empty; }
        }

        public override string Owner
        {
            get { return string.Empty; }
        }

        public override string Group
        {
            get { return string.Empty; }
        }

        public override string Permissions
        {
            get { return string.Empty; }
        }

        public override bool IsEnumerable
        {
            get { return false; }
        }

        #endregion

        public override void ExecuteDefaultAction()
        {
            if (Parent != null)
            {
                var grantParent = Parent.Parent; // since we are child of an selected item, we need to select the grant-parent
                if (grantParent != null)
                {
                    grantParent.IsSelected = true;
                }
            }
        }
    }
}
