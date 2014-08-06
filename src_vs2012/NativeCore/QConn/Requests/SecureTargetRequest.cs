using System;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Requests
{
    /// <summary>
    /// Base class for all requests send by QConnDoor to target device.
    /// They are all supposed to open another secure communication channel.
    /// </summary>
    abstract class SecureTargetRequest
    {
        private const ushort TargetVersion = 2;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetRequest(ushort version, ushort code)
        {
            Version = version;
            Code = code;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public SecureTargetRequest(ushort code)
        {
            Version = TargetVersion;
            Code = code;
        }

        #region Properties

        /// <summary>
        /// Gets the version of the protocol.
        /// </summary>
        public ushort Version
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command code of the request.
        /// </summary>
        public ushort Code
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Gets data to send to target.
        /// </summary>
        public byte[] GetData()
        {
            byte[] payload = GetPayload();
            ushort payloadSize = payload != null ? (ushort) payload.Length : (ushort) 0;

            // prepare header:
            var data = new byte[payloadSize + 6];
            BitHelper.BigEndian_Set(data, 0, (ushort) data.Length);
            BitHelper.BigEndian_Set(data, 2, Version);
            BitHelper.BigEndian_Set(data, 4, Code);

            // copy the payload, if given:
            if (payload != null && payloadSize > 0)
            {
                Array.Copy(payload, 0, data, 6, payloadSize);
            }

            return data;
        }

        /// <summary>
        /// Gets the payload to send inside the request to target.
        /// </summary>
        protected abstract byte[] GetPayload();
    }
}
