using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.Package.Helpers
{
    /// <summary>
    /// Helper class to load system icons for files and folders.
    /// Original code from Microsoft sample at: http://support.microsoft.com/kb/319350
    /// </summary>
    internal static class IconHelper
    {
        #region P/Invoke

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFileInfo psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SHFileInfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_SMALLICON = 0x1;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_OPENICON = 0x02;

        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        public const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        public const uint FILE_ATTRIBUTE_FILE = 0x100;

        #endregion

        /// <summary>
        /// Creates new instance of an image-source with icon for specified file type or folder.
        /// </summary>
        public static ImageSource GetIcon(string extension, bool isSmall, bool isDirectory, bool isOpen)
        {
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | (isSmall ? SHGFI_SMALLICON : SHGFI_LARGEICON);
            var attribute = (isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_FILE) | (isOpen ? SHGFI_OPENICON : 0u);
            var shfi = new SHFileInfo();

            var result = SHGetFileInfo(extension, attribute, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            if (result == IntPtr.Zero)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

            try
            {
                return ConvertToImageSource(shfi.hIcon);
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load icon for: " + extension);
                throw;
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }

        private static ImageSource ConvertToImageSource(IntPtr handle)
        {
            using (Icon i = Icon.FromHandle(handle))
            {
                var img = Imaging.CreateBitmapSourceFromHIcon(i.Handle, new Int32Rect(0, 0, i.Width, i.Height), BitmapSizeOptions.FromEmptyOptions());
                img.Freeze();
                return img;
            }
        }
    }
}
