using System;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class MessageViewItem : BaseViewItem
    {
        private readonly string _message;

        public MessageViewItem(TargetNavigatorViewModel viewModel, string message)
            : base(viewModel)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            _message = message;
        }

        public MessageViewItem(TargetNavigatorViewModel viewModel, Exception ex)
            : base(viewModel)
        {
            if (ex == null)
                throw new ArgumentNullException("ex");

            _message = ex.Message;
        }

        #region Properties

        public override string Name
        {
            get { return _message; }
        }

        #endregion
    }
}
