using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using Microsoft.VisualStudio.Shell;

namespace BlackBerry.Package.Options
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

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string NdkPath
        {
            get { return Control.NdkPath; }
            set { Control.NdkPath = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ToolsPath
        {
            get { return Control.ToolsPath; }
            set { Control.ToolsPath = value; }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string JavaHomePath
        {
            get { return Control.JavaHomePath; }
            set { Control.JavaHomePath = value; }
        }

        public bool IsOpeningExternal
        {
            get { return Control.IsOpeningExternal; }
            set { Control.IsOpeningExternal = value; }
        }

        #endregion

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            NdkPath = ConfigDefaults.NdkDirectory;
            ToolsPath = ConfigDefaults.ToolsDirectory;
            JavaHomePath = ConfigDefaults.JavaHome;
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
            if (e.ApplyBehavior == ApplyKind.Apply)
            {
                ConfigDefaults.Apply(NdkPath, ToolsPath, JavaHomePath);
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            Control.OnReset();
        }
    }
}
