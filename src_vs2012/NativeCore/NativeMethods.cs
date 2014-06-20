using System.Runtime.InteropServices;

namespace BlackBerry.NativeCore
{
    /// <summary>
    /// Class collecting P/Invoke definitions.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Indicates whether the file type is binary or not.
        /// </summary>
        /// <param name="applicationName">Full path to the file to check</param>
        /// <param name="binaryType">If file is binary the bitness of the app is indicated by lpBinaryType value.</param>
        /// <returns>True if the file is binary false otherwise</returns>
        [DllImport("kernel32.dll")]
        public static extern bool GetBinaryType([MarshalAs(UnmanagedType.LPWStr)] string applicationName, out uint binaryType);
    }
}
