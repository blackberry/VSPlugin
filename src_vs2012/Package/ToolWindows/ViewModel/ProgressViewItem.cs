using System;
using System.Windows.Media;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class ProgressViewItem : BaseViewItem
    {
        private readonly string _title;

        public ProgressViewItem(string title)
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

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        #endregion
    }
}
