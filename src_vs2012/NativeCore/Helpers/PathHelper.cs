using System;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class for path operations.
    /// </summary>
    static class PathHelper
    {
        /// <summary>
        /// Gets the last non-empty item of the path.
        /// </summary>
        public static string ExtractName(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // skip last path separators:
            int end = path.Length - 1;
            while (end >= 0 && IsPathSeparator(path[end]))
                end--;
            if (end < 0)
                return null;

            // locate the segment beginning:
            int start = end;
            while (start >= 0 && !IsPathSeparator(path[start]))
                start--;

            if (start < 0)
                return path.Substring(0, end + 1);
            return path.Substring(start + 1, end - start);
        }

        private static bool IsPathSeparator(char c)
        {
            return c == '/' || c == '\\';
        }

        /// <summary>
        /// Gets the beginning of the path without the name.
        /// </summary>
        public static string ExtractDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            // skip last path separators:
            int end = path.Length - 1;
            while (end >= 0 && IsPathSeparator(path[end]))
                end--;
            if (end < 0)
                return "/";

            // locate the segment beginning:
            int start = end;
            while (start >= 0 && !IsPathSeparator(path[start]))
                start--;

            if (start <= 0)
                return "/";

            return path.Substring(0, start);
        }

        /// <summary>
        /// Concatenates two path segments.
        /// </summary>
        public static string MakePath(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (path[path.Length - 1] == '/')
            {
                if (name[0] == '/')
                    return path + name.Substring(1);
                return path + name;
            }

            if (name[0] == '/')
                return path + name;
            return string.Concat(path, "/", name);
        }
    }
}
