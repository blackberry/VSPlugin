using System.Diagnostics;
using System.Windows.Forms;
using RIM.VSNDK_Package.Options.Dialogs;

namespace RIM.VSNDK_Package.Options
{
    public partial class TargetsOptionControl : UserControl
    {
        public TargetsOptionControl()
        {
            InitializeComponent();
        }

        private void lnkMoreInfo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://developer.blackberry.com/native/documentation/cascades/getting_started/setting_up.html");
        }

        private void bttAdd_Click(object sender, System.EventArgs e)
        {
            var form = new DeviceForm("Add new Target Device");

            if (form.ShowDialog() == DialogResult.OK)
            {
                
            }
        }
    }
}
