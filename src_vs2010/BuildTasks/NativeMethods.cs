using System.Runtime.InteropServices;
using System.Text;

namespace BlackBerry.BuildTasks
{
    internal static class NativeMethods
    {
        /// <summary>
        /// Interface to unmanaged code for getting the ShortPathName for a given directory.
        /// </summary>
        /// <param name="path">Path to be converted</param>
        /// <param name="shortPath">Returned ShortPathName</param>
        /// <param name="shortPathLength">Length of the ShortPathName</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetShortPathName(
                 [MarshalAs(UnmanagedType.LPTStr)]
                   string path,
                 [MarshalAs(UnmanagedType.LPTStr)]
                   StringBuilder shortPath,
                 int shortPathLength);
        /// <summary> GDB works with short path names only, which requires converting the path names to/from long ones. This function 
        /// returns the long path name for a given short one. </summary>
        /// <param name="path">Short path name. </param>
        /// <param name="longPath">Returns this long path name. </param>
        /// <param name="longPathLength"> Lenght of this long path name. </param>
        /// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern int GetLongPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
            string path,
            [MarshalAs(UnmanagedType.LPTStr)]
            StringBuilder longPath,
            int longPathLength);

        /// <summary>
        /// Gets the short version of specified path.
        /// </summary>
        public static string GetShortPathName(string path)
        {
            const int size = 1024;
            var buffer = new StringBuilder(size);

            do
            {
                int result = GetShortPathName(path, buffer, buffer.Capacity);

                // did the call failed to convert the path?
                if (result == 0)
                    return null;
                // was the buffer too small:
                if (result == buffer.Capacity)
                {
                    buffer.Capacity += size;
                    continue;
                }

                return buffer.ToString(0, result);
            } while (false);

            return null;
        }

        /// <summary>
        /// Gets the long version of specified path.
        /// </summary>
        public static string GetLongPathName(string path)
        {
            const int size = 1024;
            var buffer = new StringBuilder(size);

            do
            {
                int result = GetLongPathName(path, buffer, buffer.Capacity);

                // did the call failed to convert the path?
                if (result == 0)
                    return null;
                // was the buffer too small:
                if (result == buffer.Capacity)
                {
                    buffer.Capacity += size;
                    continue;
                }

                return buffer.ToString(0, result);
            } while (false);

            return null;
        }
    }
}
