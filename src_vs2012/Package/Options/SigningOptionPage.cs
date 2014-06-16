using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
{
    /// <summary>
    /// Option page to manage developer signing process.
    /// </summary>
    [Guid("db5d17b9-8973-498b-a32d-194e8b31e431")]
    public sealed class SigningOptionPage : DialogPage
    {
        #region Control

        private SigningOptionControl _control;

        private SigningOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new SigningOptionControl();
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

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            Control.OnActivate();
        }
    }
}
