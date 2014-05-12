using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace RIM.VSNDK_Package.Options
{
    [Guid("4dbdc58d-98ec-4908-b7e7-0638c71eae81")]
    public class GeneralOptionPage : DialogPage
    {
        private GeneralOptionControl _optionsWindow;

        private GeneralOptionControl GeneralControl
        {
            get
            {
                if (_optionsWindow == null)
                {
                    _optionsWindow = new GeneralOptionControl();
                    _optionsWindow.Location = new Point(0, 0);
                }

                return _optionsWindow;
            }
        }

        [Browsable(false)]
        protected override IWin32Window Window
        {
            get
            {
                return GeneralControl;
            }
        }
    }
}
