using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using BlackBerry.NativeCore;
using BlackBerry.Package.Helpers;

namespace BlackBerry.Package.Dialogs
{
    internal partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            pictureLogo.Image = LoadLogoImage();
            lblVersion.Text = LoadVersion();
        }

        private Image LoadLogoImage()
        {
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            string logoResourceName = FindLogoResource(resources);

            // and load logo image:
            if (!string.IsNullOrEmpty(logoResourceName))
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logoResourceName))
                {
                    if (stream != null)
                    {
                        return new Bitmap(stream);
                    }
                }
            }

            return null;
        }

        private static string FindLogoResource(IEnumerable<string> resources)
        {
            if (resources == null)
                return null;

            foreach (var name in resources)
            {
                if (name.EndsWith(".Logo.png"))
                    return name;
            }

            return null;
        }

        private string LoadVersion()
        {
            var name = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            return "Version: v" + name.Version;
        }

        private void linkSourceCode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL(ConfigDefaults.GithubProjectSourceCode);
        }

        private void linkBugTracker_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL(ConfigDefaults.GithubProjectIssues);
        }

        private void linkAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL(ConfigDefaults.GithubProjectAuthor);
        }

        private void linkTwitter_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DialogHelper.StartURL(ConfigDefaults.GithubProjectAuthorTwitter);
        }
    }
}
