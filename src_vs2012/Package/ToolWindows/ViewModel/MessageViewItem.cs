using System;
using System.Windows.Media;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    public sealed class MessageViewItem : BaseViewItem
    {
        private readonly string _message;

        public MessageViewItem(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            _message = message;
        }

        public MessageViewItem(Exception ex)
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

        public override ImageSource ImageSource
        {
            get { return null; }
        }

        #endregion
    }
}
