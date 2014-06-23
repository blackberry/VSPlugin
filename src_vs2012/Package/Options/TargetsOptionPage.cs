using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
{
    /// <summary>
    /// Option page to manage list of targets and debug-tokens.
    /// </summary>
    [Guid("acd13513-da2b-4862-a4fc-30672233d812")]
    public sealed class TargetsOptionPage : DialogPage
    {
        #region Control

        private TargetsOptionControl _control;

        private TargetsOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new TargetsOptionControl();
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

        protected override void OnApply(PageApplyEventArgs e)
        {
            Control.OnApply();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Control.OnReset();
        }
    }
}
