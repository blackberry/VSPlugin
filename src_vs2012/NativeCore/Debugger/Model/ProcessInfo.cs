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
            ExecutablePath = executablePath;
        }

        #region Properties

        public uint ID
        {
            get;
            private set;
        }

        public string ExecutablePath
        {
            get;
            private set;
        }

        #endregion
    }
}
