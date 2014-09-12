using System;
using System.Collections.Generic;
using System.Threading;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn;

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
            private AutoResetEvent _hasStatusEvent;
            private readonly string _sshPublicKeyPath;

            public event EventHandler<TargetConnectionEventArgs> StatusChanged;

            public TargetInfo(DeviceDefinition device, string sshPublicKeyPath)
            {
                if (device == null)
                    throw new ArgumentNullException("device");
                if (string.IsNullOrEmpty(sshPublicKeyPath))
                    throw new ArgumentNullException("sshPublicKeyPath");

                Status = TargetStatus.Initialized;
                Device = device;
                _hasStatusEvent = new AutoResetEvent(false);
                _sshPublicKeyPath = sshPublicKeyPath;

                Client = new QConnClient();
                Door = new QConnDoor();
                Door.Authenticated += QConnDoorAuthenticationChanged;
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

            public QConnDoor Door
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

            #endregion

            #region IDisposable Implementation

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_hasStatusEvent != null)
                    {
                        _hasStatusEvent.Dispose();
                        _hasStatusEvent = null;
                    }

                    if (Client != null)
                    {
                        Client.Dispose();
                        Client = null;
                    }

                    if (Door != null)
                    {
                        Door.Dispose();
                        Door = null;
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

            public bool Wait(int millisecondsTimeout)
            {
                if (_hasStatusEvent == null)
                    throw new ObjectDisposedException("TargetInfo");

                return _hasStatusEvent.WaitOne(millisecondsTimeout);
            }

            public void Start()
            {
                NotifyStatusChange(TargetStatus.Connecting, null);

                /////////////////////////////////////
                // Trick of the day: there can be only one QConnDoor connection setup to the device
                // so it might happen, we already opened one in Visual Studio, Momentics or on
                // another machine; so let's try, if it's possible to communicate with device services.
                try
                {
                    Client.Load(Device.IP, QConnClient.DefaultPort, 1000);

                    // all is fine, connection established, no need to have own QConnDoor
                    NotifyStatusChange(TargetStatus.Connected, null);
                    return;
                }
                catch
                {
                    // invalid device or QConnDoor not opened by others...
                }

                Door.OpenAsync(Device.IP, Device.Password, _sshPublicKeyPath);
            }

            private void QConnDoorAuthenticationChanged(object sender, QConnAuthenticationEventArgs e)
            {
                TraceLog.WriteLine("Target status changed to: {0} for: {1}", e.IsAuthenticated ? "authenticated" : "disconnected", Device);

                if (e.IsAuthenticated)
                {
                    // load info about services:
                    try
                    {
                        Client.Load(Device.IP);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Failed to load info about target's services");
                        NotifyStatusChange(TargetStatus.Failed, "Failed to load info about services on target");
                    }

                    // start keeping-alive the connection:
                    try
                    {
                        Door.KeepAlive(QConnDoor.DefaultKeepAliveInterval); // this will continuously asynchronously send the keep-alive request
                        NotifyStatusChange(TargetStatus.Connected, null);
                    }
                    catch (Exception ex)
                    {
                        TraceLog.WriteException(ex, "Failed to start keeping the connection alive");
                        NotifyStatusChange(TargetStatus.Failed, "Lost connection to the device");
                    }
                }
                else
                {
                    if (Status == TargetStatus.Connected)
                    {
                        NotifyStatusChange(TargetStatus.Failed, "Lost connection to the device");
                    }
                    else
                    {
                        NotifyStatusChange(TargetStatus.Failed, "Failed to connect to the device");
                    }
                }

                // then notify Targets class, so it updates the internal state:
                OnChildConnectionStatusChanged(this, Status);
            }

            private void NotifyStatusChange(TargetStatus status, string message)
            {
                if (status != Status)
                {
                    Status = status;

                    var handler = StatusChanged;
                    if (handler != null)
                    {
                        handler(this, new TargetConnectionEventArgs(Device, Client, status, message));
                    }
                }

                // and wake up waiters, if needed...
                if (status != TargetStatus.Connecting)
                {
                    if (_hasStatusEvent != null)
                    {
                        _hasStatusEvent.Set();
                    }
                }
            }

            public TargetConnectionEventArgs ToEventArgs()
            {
                return new TargetConnectionEventArgs(Device, Client, Status, null);
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
            return connection != null && connection.Status == TargetStatus.Connected;
        }

        /// <summary>
        /// Gets an indication, if there is already a valid connection to the device with given IP.
        /// </summary>
        public static bool IsConnected(DeviceDefinition device)
        {
            return device != null && IsConnected(device.IP);
        }

        /// <summary>
        /// Gets the manager of the device for advanced services.
        /// </summary>
        public static QConnClient Get(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            return Get(device.IP);
        }

        /// <summary>
        /// Gets the manager of the device for advanced services.
        /// </summary>
        public static QConnClient Get(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            var existingTarget = Find(ip);
            if (existingTarget != null && existingTarget.Status == TargetStatus.Connected)
            {
                return existingTarget.Client;
            }

            return null;
        }

        /// <summary>
        /// Gets the manager of the device for advanced services.
        /// It will try to automatically connect to the device, if needed and might block current thread for that time.
        /// </summary>
        public static QConnClient Get(DeviceDefinition device, string sshPublicKeyPath)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(sshPublicKeyPath))
                throw new ArgumentNullException(sshPublicKeyPath);

            var qClient = Get(device.IP);

            // need to connect first:
            if (qClient == null)
            {
                Connect(device, sshPublicKeyPath, null);

                // wait until link established or error:
                if (Wait(device.IP))
                {
                    qClient = Get(device.IP);
                }
            }

            return qClient;
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
        public static void Connect(DeviceDefinition device, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            Connect(device, ConfigDefaults.SshPublicKeyPath, resultHandler);
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

                    // until we open the QConnDoor, it might report 'disconnected' state, that's why it's inside lock
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
                    resultHandler.BeginInvoke(null, existingTarget.ToEventArgs(), StatusHandlerAsyncCleanup, resultHandler);
                }
            }
        }

        private static void StatusHandlerAsyncCleanup(IAsyncResult ar)
        {
            var resultHandler = (EventHandler<TargetConnectionEventArgs>) ar.AsyncState;

            try
            {
                resultHandler.EndInvoke(ar);
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Targets status-change notification failed");
            }
        }

        private static void OnChildConnectionStatusChanged(TargetInfo target, TargetStatus status)
        {
            if (target == null)
                throw new ArgumentNullException("sender");

            bool dispose = false;

            if (status == TargetStatus.Disconnected || status == TargetStatus.Failed)
            {
                lock (_sync)
                {
                    dispose = _activeTargets.Remove(target);
                }
            }

            // only release the object, when it belong to the list
            // (to avoid double-releases, in case external handlers manipulated the list):
            if (dispose)
            {
                // and release resources:
                target.Dispose();
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

        /// <summary>
        /// Waits until connection status changes to connected or failed for a given device.
        /// </summary>
        public static void Wait(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            Wait(device.IP);
        }

        /// <summary>
        /// Waits until connection status changes to connected or failed for a given device.
        /// </summary>
        public static bool Wait(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            return Wait(GdbProcessor.ShortInfinite, ip);
        }

        /// <summary>
        /// Waits until connection status changes to connected or failed for a given device.
        /// </summary>
        public static bool Wait(int millisecondsTimeout, DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            return Wait(millisecondsTimeout, device.IP);
        }

        /// <summary>
        /// Waits until connection status changes to connected or failed for a given device.
        /// </summary>
        public static bool Wait(int millisecondsTimeout, string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            var existingTarget = Find(ip);
            if (existingTarget != null)
            {
                return existingTarget.Wait(millisecondsTimeout);
            }

            return false;
        }
    }
}
