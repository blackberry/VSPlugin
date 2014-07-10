using System;

namespace BlackBerry.NativeCore.Debugger.Model
{
    /// <summary>
    /// Description of the process received from the device.
    /// </summary>
    public sealed class ProcessInfo
    {
        /// <summary>
        /// Init constructor, designated for GDB response.
        /// </summary>
        public ProcessInfo(uint id, string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                throw new ArgumentNullException("executablePath");

            ID = id;
            Name = ExtractName(executablePath);
            ExecutablePath = executablePath;
            ShortExecutablePath = ExtractShortPath(executablePath);
        }

        /// <summary>
        /// Gets the name of the process, based on the executable path.
        /// Simply - just grabs the last item of the path.
        /// </summary>
        private static string ExtractName(string executablePath)
        {
            bool skipLastChar = false;

            for (int i = executablePath.Length - 1; i >= 0; i--)
            {
                if (executablePath[i] == '/' || executablePath[i] == '\\')
                {
                    if (i == executablePath.Length - 1)
                    {
                        skipLastChar = true;
                    }
                    else
                    {
                        if (skipLastChar)
                            return executablePath.Substring(i + 1, executablePath.Length - i - 2);
                        return executablePath.Substring(i + 1);
                    }
                }
            }

            if (skipLastChar)
                return executablePath.Substring(0, executablePath.Length - 1);
            return executablePath;
        }

        /// <summary>
        /// Gets the short version of the path to the executable.
        /// </summary>
        private static string ExtractShortPath(string executablePath)
        {
            const string startToken = "accounts/1000/appdata/";
            // is it a sandbox application?
            if (executablePath.StartsWith(startToken, StringComparison.OrdinalIgnoreCase))
                return executablePath.Substring(startToken.Length);

            return executablePath;
        }

        #region Properties

        public uint ID
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string ExecutablePath
        {
            get;
            private set;
        }

        public string ShortExecutablePath
        {
            get;
            private set;
        }

        #endregion
    }
}
