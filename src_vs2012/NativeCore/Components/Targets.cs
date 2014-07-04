using System;
using System.Collections.Generic;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.Tools;

namespace BlackBerry.NativeCore.Components
{
    /// <summary>
    /// Tracker of already connected and disconnected devices.
    /// </summary>
    public static class Targets
    {
        #region Internal Classes

        /// <summary>
        /// Class providing help information about connection parameters with the target device.
        /// </summary>
        sealed class TargetInfo : IDisposable
        {
            public event EventHandler<TargetConnectionEventArgs> StatusChanged;

            public TargetInfo(DeviceDefinition device, string sshPublicKeyPath)
            {
                if (device == null)
                    throw new ArgumentNullException("device");
                if (string.IsNullOrEmpty(sshPublicKeyPath))
                    throw new ArgumentNullException("sshPublicKeyPath");

                Device = device;
                Runner = new DeviceConnectRunner(ConfigDefaults.ToolsDirectory, Device.IP, Device.Password, sshPublicKeyPath);
                Runner.StatusChanged += OnConnectionStatusChanged;
            }

            private void OnConnectionStatusChanged(object sender, EventArgs eventArgs)
            {
                var statusHandler = StatusChanged;
                if (statusHandler != null)
                {
                    statusHandler(null, ToEventArgs());
                }
            }

            ~TargetInfo()
            {
                Dispose(false);
            }

            #region Properties

            public DeviceDefinition Device
            {
                get;
                private set;
            }

            public DeviceConnectRunner Runner
            {
                get;
                private set;
            }

            #endregion

            #region IDisposable Implementation

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (Runner != null)
                    {
                        Runner.Dispose();
                        Runner = null;
                    }

                    StatusChanged = null;
                }
            }

            #endregion

            public bool HasIdenticalIP(string ip)
            {
                if (ip == null)
                    return false;

                return string.CompareOrdinal(Device.IP, ip) == 0;
            }

            public void Start()
            {
                if (Runner == null)
                    throw new ObjectDisposedException("ConnectionInfo");
                Runner.ExecuteAsync();
            }

            public TargetConnectionEventArgs ToEventArgs()
            {
                if (Runner == null)
                    return new TargetConnectionEventArgs(Device, TargetStatus.Failed, "Unable to start the connection");

                // is the process still running?
                if (!Runner.IsProcessing)
                    return new TargetConnectionEventArgs(Device, TargetStatus.Disconnected, "Disconnected");

                if (Runner.IsConnectionFailed)
                    return new TargetConnectionEventArgs(Device, TargetStatus.Failed, Runner.LastError);

                if (Runner.IsConnected)
                    return new TargetConnectionEventArgs(Device, TargetStatus.Connected, "Connected");

                if (Runner.IsProcessing)
                    return new TargetConnectionEventArgs(Device, TargetStatus.Connecting, "Connecting...");

                throw new InvalidOperationException("Impossible to determine status of the target device");
            }
        }

        #endregion

        private static readonly List<TargetInfo> _activeTargets;
        private static readonly object _sync;

        static Targets()
        {
            _activeTargets = new List<TargetInfo>();
            _sync = new object();
        }

        private static TargetInfo NoSyncFind(string ip)
        {
            foreach (var target in _activeTargets)
                if (target.HasIdenticalIP(ip))
                    return target;

            return null;
        }

        /// <summary>
        /// Finds info about connection to the device with specified IP.
        /// </summary>
        private static TargetInfo Find(string ip)
        {
            lock (_sync)
            {
                return NoSyncFind(ip);
            }
        }

        /// <summary>
        /// Gets an indication, if there is already a valid connection to the device with given IP.
        /// </summary>
        public static bool IsConnected(string ip)
        {
            var connection = Find(ip);
            return connection != null && connection.Runner.IsConnected;
        }

        /// <summary>
        /// Gets an indication, if there is already a valid or broken connection to the device with given IP.
        /// </summary>
        public static bool IsConnectedOrFailed(string ip)
        {
            var connection = Find(ip);
            return connection == null || connection.Runner.IsConnected || connection.Runner.IsConnectionFailed;
        }

        /// <summary>
        /// Requests secure connection setup to given device.
        /// </summary>
        public static void Connect(string ip, string password, DeviceDefinitionType type, string sshPublicKeyPath, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            Connect(new DeviceDefinition("Ad-hoc device", ip, password, type), sshPublicKeyPath, resultHandler);
        }

        /// <summary>
        /// Requests secure connection setup to given device.
        /// </summary>
        public static void Connect(DeviceDefinition device, string sshPublicKeyPath, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(sshPublicKeyPath))
                throw new ArgumentNullException("sshPublicKeyPath");

            TargetInfo existingTarget;
            lock (_sync)
            {
                existingTarget = NoSyncFind(device.IP);

                // this trick will let only one thread add new connection-info, if both blocked on the sync above:
                if (existingTarget == null)
                {
                    var newTarget = new TargetInfo(device, sshPublicKeyPath);
                    if (resultHandler != null)
                    {
                        newTarget.StatusChanged += resultHandler;
                    }
                    _activeTargets.Add(newTarget);

                    // until we start the tool, it might report 'disconnected' state, that's why it's inside lock
                    newTarget.Start();
                }
                else
                {
                    if (resultHandler != null)
                    {
                        existingTarget.StatusChanged += resultHandler;
                    }
                }
            }

            // check if already connected:
            if (existingTarget != null)
            {
                // and notify the caller:
                if (resultHandler != null)
                {
                    resultHandler.BeginInvoke(null, existingTarget.ToEventArgs(), null, null);
                }
            }
        }

        /// <summary>
        /// Stops receiving more events related to the connection state to the target device.
        /// </summary>
        public static bool Unsubscribe(DeviceDefinition device, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (resultHandler == null)
                throw new ArgumentNullException("resultHandler");
            return Unsubscribe(device.IP, resultHandler);
        }

        /// <summary>
        /// Stops receiving more events related to the connection state to the target device.
        /// </summary>
        public static bool Unsubscribe(string ip, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (resultHandler == null)
                throw new ArgumentNullException("resultHandler");

            TargetInfo existingTarget;

            lock (_sync)
            {
                existingTarget = NoSyncFind(ip);
                if (existingTarget != null)
                {
                    existingTarget.StatusChanged -= resultHandler;
                }
            }

            return existingTarget != null;
        }

        /// <summary>
        /// Requests closing secure connection to given device.
        /// </summary>
        public static bool Disconnect(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            TargetInfo existingTarget;
            bool result = false;

            lock (_sync)
            {
                existingTarget = NoSyncFind(ip);
                if (existingTarget != null)
                {
                    result = _activeTargets.Remove(existingTarget);
                }
            }

            // and release resources:
            if (existingTarget != null)
            {
                existingTarget.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Requests closing secure connection to given device.
        /// </summary>
        public static bool Disconnect(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            return Disconnect(device.IP);
        }
    }
}
