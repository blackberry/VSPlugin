using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Info about process running on the device.
    /// </summary>
    public sealed class SystemInfoProcess
    {
        /// <summary>
        /// Init constuctor.
        /// </summary>
        public SystemInfoProcess(uint id, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            ID = id;
            Name = name;
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

        #endregion

        public override string ToString()
        {
            return Name;
        }
    }
}
