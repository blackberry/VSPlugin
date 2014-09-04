using BlackBerry.NativeCore.Debugger.Model;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Info about process running on the device.
    /// </summary>
    public sealed class SystemInfoProcess : ProcessInfo
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SystemInfoProcess(uint id, uint parentID, string executablePath)
            : base(id, executablePath)
        {
            ParentID = parentID;
        }

        #region Properties

        public uint ParentID
        {
            get;
            private set;
        }

        #endregion
    }
}
