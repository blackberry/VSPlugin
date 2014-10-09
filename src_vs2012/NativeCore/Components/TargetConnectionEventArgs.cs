using System;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn;

namespace BlackBerry.NativeCore.Components
{
    /// <summary>
    /// Arguments passed along with Targets class events.
    /// </summary>
    public sealed class TargetConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetConnectionEventArgs(DeviceDefinition device, QConnClient client, TargetStatus status, string message)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            Device = device;
            Client = client;
            Status = status;
            Message = message;
        }

        #region Properties

        public DeviceDefinition Device
        {
            get;
            private set;
        }

        public QConnClient Client
        {
            get;
            private set;
        }

        public TargetStatus Status
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        #endregion
    }
}
