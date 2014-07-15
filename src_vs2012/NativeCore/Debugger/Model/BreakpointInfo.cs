namespace BlackBerry.NativeCore.Debugger.Model
{
    /// <summary>
    /// Description of the breakpoint received from the GDB.
    /// </summary>
    public sealed class BreakpointInfo
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public BreakpointInfo(uint id, string fileName, uint line, uint address)
        {
            ID = id;
            FileName = fileName ?? string.Empty;
            LongFileName = string.IsNullOrEmpty(fileName) ? string.Empty : NativeMethods.GetLongPathName(fileName);
            Line = line;
            Address = address;
        }

        #region Properties

        /// <summary>
        /// Gets the breakpoint ID in GDB.
        /// </summary>
        public uint ID
        {
            get;
            private set;
        }

        public string FileName
        {
            get;
            private set;
        }

        public string LongFileName
        {
            get;
            private set;
        }

        /// <summary>
        /// Line number within the file.
        /// </summary>
        public uint Line
        {
            get;
            private set;
        }

        public uint Address
        {
            get;
            private set;
        }

        #endregion
    }
}
