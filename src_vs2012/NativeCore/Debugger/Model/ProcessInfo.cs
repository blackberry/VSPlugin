using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.Debugger.Model
{
    /// <summary>
    /// Description of the process received from the device.
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// Init constructor, designated for GDB response.
        /// </summary>
        public ProcessInfo(uint id, string executablePath)
        {
            if (string.IsNullOrEmpty(executablePath))
                throw new ArgumentNullException("executablePath");

            ID = id;

            // get the name of the process, based on the full executable name, simply - just grabs the last item of the path:
            Name = PathHelper.ExtractName(executablePath);
            Directory = PathHelper.ExtractDirectory(executablePath);
            ExecutablePath = executablePath;
            ShortExecutablePath = ExtractShortPath(executablePath);
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

        public string Directory
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

        public override string ToString()
        {
            return string.Concat("0x", ID.ToString("X8"), " ", Name, " (", ExecutablePath, ")");
        }

        /// <summary>
        /// Searches for a process with specified executable (full name or partial).
        /// It will return null, if not found.
        /// </summary>
        public static ProcessInfo Find(IEnumerable<ProcessInfo> processes, string executable)
        {
            if (processes == null)
                throw new ArgumentNullException("processes");
            if (string.IsNullOrEmpty(executable))
                throw new ArgumentNullException("executable");

            // first try to find identical executable:
            foreach (var process in processes)
            {
                if (string.Compare(process.ExecutablePath, executable, StringComparison.OrdinalIgnoreCase) == 0)
                    return process;
            }

            // is the name matching:
            foreach (var process in processes)
            {
                if (string.Compare(process.Name, executable, StringComparison.OrdinalIgnoreCase) == 0)
                    return process;
            }

            // or maybe only ends with it?
            foreach (var process in processes)
            {
                if (process.ExecutablePath != null && process.ExecutablePath.EndsWith(executable, StringComparison.OrdinalIgnoreCase))
                    return process;
            }

            return null;
        }
    }
}
