using System;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    internal sealed class ProgressViewItem : BaseViewItem
    {
        private readonly string _title;

        public ProgressViewItem(TargetNavigatorViewModel viewModel, string title)
            : base(viewModel)
        {
            if (string.IsNullOrEmpty(title))
                throw new ArgumentNullException("title");

            _title = title;
        }

        #region Properties

        public override string Name
        {
            get { return _title; }
        }

        public override bool IsEnumerable
        {
            get { return false; }
        }

        #endregion
    }
}
