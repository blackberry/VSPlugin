using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Info about process running on the device.
    /// </summary>
    public sealed class SystemInfoProcess
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SystemInfoProcess(uint id, uint parentID, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            ID = id;
            ParentID = parentID;
            Name = name;
        }

        #region Properties

        public uint ID
        {
            get;
            private set;
        }

        public uint ParentID
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
