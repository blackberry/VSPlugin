using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace RIM.VSNDK_Package.Options
{
    /// <summary>
    /// Option page to manage general config - like PATHs!.
    /// </summary>
    [Guid("4dbdc58d-98ec-4908-b7e7-0638c71eae81")]
    public sealed class GeneralOptionPage : DialogPage
    {
        #region Control

        private GeneralOptionControl _control;

        private GeneralOptionControl Control
        {
            get
            {
                if (_control == null)
                {
                    _control = new GeneralOptionControl();
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

        public string NdkPath
        {
            get { return Control.NdkPath; }
            set { Control.NdkPath = value; }
        }

        public string ToolsPath
        {
            get { return Control.ToolsPath; }
            set { Control.ToolsPath = value; }
        }

        #endregion
    }
}
