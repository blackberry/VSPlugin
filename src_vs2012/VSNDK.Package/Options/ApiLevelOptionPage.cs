using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options
{
    /// <summary>
    /// Option page to manage API levels.
    /// </summary>
    [Guid("5389599f-f906-4c23-b976-c7d75f1ae1ce")]
    public sealed class ApiLevelOptionPage : DialogPage
    {
        #region Control

        private ApiLevelOptionControl _control;

        private ApiLevelOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new ApiLevelOptionControl();
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

            if (!Control.HasSelectedNDK || PackageViewModel.Instance.ActiveNDK == null)
            {
                if (MessageBoxHelper.Show("No NDK is set as active. You won't be able to compile anything.\r\nDo you want to correct it manually?",
                                          "API Level Settings",
                                          MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    e.ApplyBehavior = ApplyKind.Cancel;
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            Control.OnClosed();
        }
    }
}
