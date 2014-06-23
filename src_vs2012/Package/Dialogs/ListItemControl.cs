using System;
using System.Windows.Forms;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.Package.Options.Dialogs
{
    internal partial class ListItemControl : UserControl
    {
        public event EventHandler ExecuteAction;

        private ApiLevelTask _action;

        public ListItemControl()
        {
            InitializeComponent();

            Action = ApiLevelTask.Hide;
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

        public ApiLevelTask Action
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

        public ApiLevelTarget Target
        {
            get;
            set;
        }

        public Panel ParentPanel
        {
            get;
            set;
        }

        private void OnActionChanged()
        {
            bttAction.Enabled = true;
            bttAction.Visible = true;

            switch (_action)
            {
                case ApiLevelTask.Hide:
                    ActionName = string.Empty;
                    bttAction.Visible = false;
                    break;
                case ApiLevelTask.Nothing:
                    ActionName = "Locked";
                    bttAction.Enabled = false;
                    break;
                case ApiLevelTask.Install:
                    ActionName = "Install";
                    break;
                case ApiLevelTask.InstallManually:
                    ActionName = "Download...";
                    break;
                case ApiLevelTask.AddExisting:
                    ActionName = "Add...";
                    break;
                case ApiLevelTask.Uninstall:
                    ActionName = "Uninstall";
                    break;
                case ApiLevelTask.Forget:
                    ActionName = "Forget";
                    break;
                case ApiLevelTask.Refresh:
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
