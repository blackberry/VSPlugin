using System;

namespace BlackBerry.NativeCore.QConn.Response
{
    /// <summary>
    /// Base class for all types for correct responses received from QConnDoor service running on target.
    /// </summary>
    class SecureTargetResponse : SecureTargetResult
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetResponse(byte[] data, ushort version, ushort code)
            : base(data, HResult.OK)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            Version = version;
            Code = code;
        }

        #region Properties

        public ushort Version
        {
            get;
            private set;
        }

        public ushort Code
        {
            get;
            private set;
        }

        #endregion
    }
}
