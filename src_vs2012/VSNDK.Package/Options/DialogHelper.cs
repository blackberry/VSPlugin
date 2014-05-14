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

        /// <summary>
        /// Returns preconfigured window for opening BAR files.
        /// </summary>
        public static OpenFileDialog OpenBarFile(string title, string startupPath)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = title;
            openFile.InitialDirectory = startupPath;
            openFile.DefaultExt = ".bar";
            openFile.Filter = "Bar files|*.bar|All files|*.*";
            openFile.FilterIndex = 0;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;

            return openFile;
        }
    }
}
