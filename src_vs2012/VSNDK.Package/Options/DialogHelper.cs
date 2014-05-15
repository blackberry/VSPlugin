using System.Diagnostics;
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

        /// <summary>
        /// Returns preconfigured window for saving BAR file.
        /// </summary>
        public static SaveFileDialog SaveBarFile(string title, string startupPath, string fileName)
        {
            var saveFile = new SaveFileDialog();
            saveFile.Title = title;
            saveFile.InitialDirectory = startupPath;
            saveFile.FileName = fileName;
            saveFile.DefaultExt = ".bar";
            saveFile.Filter = "Bar files|*.bar|All files|*.*";
            saveFile.FilterIndex = 0;
            saveFile.CreatePrompt = false;
            saveFile.OverwritePrompt = true;

            return saveFile;
        }

        /// <summary>
        /// Returns preconfigured window for opening ZIP files.
        /// </summary>
        public static OpenFileDialog OpenZipFile(string title)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = title;
            openFile.DefaultExt = ".bar";
            openFile.Filter = "Zip files|*.zip|All files|*.*";
            openFile.FilterIndex = 0;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;

            return openFile;
        }

        /// <summary>
        /// Returns preconfigured window for saving ZIP file.
        /// </summary>
        public static SaveFileDialog SaveZipFile(string title, string fileName)
        {
            var saveFile = new SaveFileDialog();
            saveFile.Title = title;
            saveFile.FileName = fileName;
            saveFile.DefaultExt = ".zip";
            saveFile.Filter = "Zip files|*.zip|All files|*.*";
            saveFile.FilterIndex = 0;
            saveFile.CreatePrompt = false;
            saveFile.OverwritePrompt = true;

            return saveFile;
        }


        /// <summary>
        /// Opens Windows Explorer window with specified path.
        /// </summary>
        public static void StartExplorer(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            Process.Start("Explorer.exe", "/e,\"" + path + "\"");
        }

        /// <summary>
        /// Opens Windows Explorer window with specified file selected.
        /// </summary>
        public static void StartExplorerForFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            Process.Start("Explorer.exe", "/select,\"" + path + "\"");
        }

        /// <summary>
        /// Opens a default web-browser with specified URL.
        /// </summary>
        public static void StartURL(string url)
        {
            if (string.IsNullOrEmpty(url) || !(url.StartsWith("http://") || url.StartsWith("https://")))
                return;

            Process.Start(url);
        }
    }
}
