using System;
using System.Windows.Forms;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    internal partial class ListItemControl : UserControl
    {
        public event EventHandler ExecuteAction;

        private ApiLevelActionType _action;

        public ListItemControl()
        {
            InitializeComponent();

            Action = ApiLevelActionType.Hide;
            OnActionChanged();
        }

        #region Properties

        public string Title
        {
            get { return lblTitle.Text; }
            set { lblTitle.Text = value; }
        }

        public string Details
        {
            get { return lblDetails.Text; }
            set { lblDetails.Text = value; }
        }

        public string ActionName
        {
            get { return bttAction.Text; }
            set { bttAction.Text = value; }
        }

        public ApiLevelActionType Action
        {
            get { return _action; }
            set
            {
                if (_action != value)
                {
                    _action = value;
                    OnActionChanged();
                }
            }
        }

        private void OnActionChanged()
        {
            bttAction.Enabled = true;
            bttAction.Visible = true;

            switch (_action)
            {
                case ApiLevelActionType.Hide:
                    ActionName = string.Empty;
                    bttAction.Visible = false;
                    break;
                case ApiLevelActionType.Nothing:
                    ActionName = "Locked";
                    bttAction.Enabled = false;
                    break;
                case ApiLevelActionType.Install:
                    ActionName = "Install";
                    break;
                case ApiLevelActionType.InstallManually:
                    ActionName = "Download...";
                    break;
                case ApiLevelActionType.Uninstall:
                    ActionName = "Uninstall";
                    break;
                case ApiLevelActionType.Forget:
                    ActionName = "Forget";
                    break;
                case ApiLevelActionType.Refresh:
                    ActionName = "Reload";
                    break;

                default:
                    throw new ArgumentOutOfRangeException("Specified action is not supported");
            }
        }

        public object ActionArgument
        {
            get;
            set;
        }

        #endregion

        private void bttAction_Click(object sender, EventArgs e)
        {
            var actionHandler = ExecuteAction;

            // notify, that user requested to perform the action:
            if (actionHandler != null)
                actionHandler(this, e);
        }
    }
}
