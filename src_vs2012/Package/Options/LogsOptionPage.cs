using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
{
    /// <summary>
    /// Option page to manage log settings.
    /// </summary>
    [Guid("fa49d8e4-61b9-4d8f-9872-26cf73a67687")]
    public sealed class LogsOptionPage : DialogPage
    {
        #region Control

        private LogsOptionControl _control;

        private LogsOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new LogsOptionControl();
                    _control.Location = new Point(0, 0);
                }

                return _control;
            }
        }

        [Browsable(false)]
        protected override IWin32Window Window
        {
            get
            {
                return Control;
            }
        }

        #endregion

        #region Properties

        public bool InjectLogs
        {
            get { return Control.InjectLogs; }
            set { Control.InjectLogs = value; }
        }

        public bool LimitLogs
        {
            get { return Control.LimitLogs; }
            set { Control.LimitLogs = value; }
        }

        public int LimitCount
        {
            get { return Control.LimitCount; }
            set { Control.LimitCount = value; }
        }

        public string Path
        {
            get { return Control.Path; }
            set { Control.Path = value; }
        }

        #endregion
    }
}
