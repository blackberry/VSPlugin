using System.Windows.Forms;

namespace RIM.VSNDK_Package.Options
{
    internal static class DialogHelper
    {
        /// <summary>
        /// Shows 'browse-for-folder' dialog.
        /// </summary>
        public static string BrowseForFolder(string startupPath, string description)
        {
            var browser = new FolderBrowserDialog();
            browser.ShowNewFolderButton = true;
            browser.SelectedPath = startupPath;
            browser.Description = description;

            if (browser.ShowDialog() == DialogResult.OK)
                return browser.SelectedPath;

            return startupPath;
        }
    }
}
