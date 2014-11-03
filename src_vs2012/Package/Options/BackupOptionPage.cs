using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
{
    /// <summary>
    /// Options page to present some BlackBerry related backup possibilities.
    /// </summary>
    [Guid("ca5cca60-ada6-4342-8a7b-1c258e8b3060")]
    public sealed class BackupOptionPage : DialogPage
    {
        #region Control

        private BackupOptionControl _control;

        private BackupOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new BackupOptionControl();
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
    }
}
