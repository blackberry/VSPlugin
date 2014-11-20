using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace BlackBerry.Package.Helpers
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
        /// Returns preconfigured window for opening .cproject files.
        /// </summary>
        public static OpenFileDialog OpenNativeCoreProject(string title, string startupPath)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = title;
            openFile.InitialDirectory = startupPath;
            openFile.DefaultExt = ".cproject";
            openFile.Filter = "Native Core Project|*.cproject;*.project|All files|*.*";

            openFile.FilterIndex = 0;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;

            return openFile;
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
        /// Returns preconfigured window for opening certificate files.
        /// </summary>
        public static OpenFileDialog OpenCertFile(string startupPath)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = "Opening certificate file";
            openFile.InitialDirectory = startupPath;
            openFile.DefaultExt = ".p12";
            openFile.Filter = "Certificate files|*.p12|All files|*.*";
            openFile.FilterIndex = 0;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;

            return openFile;
        }

        /// <summary>
        /// Returns preconfigured window for opening CSJ files.
        /// </summary>
        public static OpenFileDialog OpenCsjFile(string startupPath, string title)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = title ?? "Opening CSJ file";
            openFile.InitialDirectory = startupPath;
            openFile.DefaultExt = ".csj";
            openFile.Filter = "Code Signing files|*.csj;*.csk|Certificate files|*.p12|All files|*.*";
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
            saveFile.FileName = string.IsNullOrEmpty(fileName) ? string.Empty : Path.ChangeExtension(fileName, ".zip");
            saveFile.DefaultExt = ".zip";
            saveFile.Filter = "Zip files|*.zip|All files|*.*";
            saveFile.FilterIndex = 0;
            saveFile.CreatePrompt = false;
            saveFile.OverwritePrompt = true;

            return saveFile;
        }

        /// <summary>
        /// Returns preconfigured window for opening ANY files.
        /// </summary>
        public static OpenFileDialog OpenAnyFile(string title)
        {
            var openFile = new OpenFileDialog();
            openFile.Title = title;
            openFile.DefaultExt = string.Empty;
            openFile.Filter = "All files|*.*";
            openFile.FilterIndex = 1;
            openFile.CheckFileExists = true;
            openFile.CheckPathExists = true;

            return openFile;
        }

        /// <summary>
        /// Returns preconfigured window for saving ANY file.
        /// </summary>
        public static SaveFileDialog SaveAnyFile(string title, string fileName)
        {
            var saveFile = new SaveFileDialog();
            saveFile.Title = title;
            saveFile.FileName = fileName;
            saveFile.DefaultExt = string.Empty;
            saveFile.Filter = "All files|*.*";
            saveFile.FilterIndex = 1;
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

            // if specified directory doesn't exist, create it:
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch
            {
            }

            // open Explorer window with this folder:
            Process.Start("Explorer.exe", "/e,\"" + path + "\"");
        }

        /// <summary>
        /// Opens Windows Explorer window with specified file selected.
        /// </summary>
        public static void StartExplorerForFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            // if file doesn't exits, try to open its parent folder:
            if (!File.Exists(path))
            {
                StartExplorer(Path.GetDirectoryName(path));
                return;
            }

            // open Explorer window with specified file selected:
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
