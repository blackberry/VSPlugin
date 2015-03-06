using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Debugger.Model;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Model;
using BlackBerry.NativeCore.QConn;
using BlackBerry.NativeCore.QConn.Model;
using BlackBerry.NativeCore.QConn.Services;
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
        /// Method to verify, if specified log entry should be populated to the developer according to current settings.
        /// </summary>
        private delegate bool LogPredicate(TargetServiceConsoleLog service, TargetLogEntry log);

        /// <summary>
        /// Method to format the output of the log message populated to all registered outputs.
        /// </summary>
        private delegate string LogFormatter(TargetLogEntry log);

        /// <summary>
        /// Class providing help information about connection parameters with the target device.
        /// </summary>
        private sealed class TargetInfo : IDisposable
        {
            private AutoResetEvent _hasStatusEvent;
            private readonly string _sshPublicKeyPath;
            private readonly string _sshPrivateKeyPath;
            private volatile bool _startingSLog2;

            public event EventHandler<TargetConnectionEventArgs> StatusChanged;

            public TargetInfo(DeviceDefinition device, string sshPublicKeyPath, string sshPrivateKeyPath)
            {
                if (device == null)
                    throw new ArgumentNullException("device");
                if (string.IsNullOrEmpty(sshPublicKeyPath))
                    throw new ArgumentNullException("sshPublicKeyPath");
                if (string.IsNullOrEmpty(sshPrivateKeyPath))
                    throw new ArgumentNullException("sshPrivateKeyPath");

                Status = TargetStatus.Initialized;
                Device = device;
                _hasStatusEvent = new AutoResetEvent(false);
                _sshPublicKeyPath = sshPublicKeyPath;
                _sshPrivateKeyPath = sshPrivateKeyPath;

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

            public TargetProcessSLog2Info SLog2Info
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

                    StopLogServices();

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
                // Verify, if we are able to connect at all, where the public key is required:
                if (!File.Exists(_sshPublicKeyPath))
                {
                    var ndk = NdkDefinition.Load();
                    if (ndk == null || !Directory.Exists(ndk.HostPath))
                    {
                        NotifyStatusChange(TargetStatus.Failed, "Unable to create public SSH key to establish connection. Download BlackBerry Native SDK first.");
                        return;
                    }

                    // generate SSH public key blocking current thread till completed:
                    var tool = new SshCreateKeyRunner(Path.Combine(ndk.HostPath, "usr", "bin", "ssh-keygen.exe"), _sshPublicKeyPath, _sshPrivateKeyPath, 4096);
                    if (!tool.Execute())
                    {
                        NotifyStatusChange(TargetStatus.Failed, "Public SSH key creation failed.");
                        return;
                    }
                }

                /////////////////////////////////////
                // Trick of the day: there can be only one QConnDoor connection setup to the device
                // so it might happen, we already opened one in Visual Studio, Momentics or on
                // another machine; so let's try, if it's possible to communicate with device services.
                try
                {
                    Client.Load(Device.IP, QConnClient.DefaultPort, 1000);
                    InitializeLogServices();

                    // all is fine, connection established, no need to have own QConnDoor
                    NotifyStatusChange(TargetStatus.Connected, null);
                    return;
                }
                catch
                {
                    // invalid device nor QConnDoor not yet opened by others...
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
                        InitializeLogServices();
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
                        NotifyStatusChange(TargetStatus.Disconnected, "Device has been disconnected");
                    }
                    else
                    {
                        NotifyStatusChange(TargetStatus.Failed, "Failed to connect to the device");
                    }
                }

                // then notify Targets class, so it updates the internal state:
                OnChildConnectionStatusChanged(this, Status);
            }

            public bool StopLogServices()
            {
                bool disposed = false;

                var slog2 = SLog2Info;
                SLog2Info = null;

                if (slog2 != null)
                {
                    if (Client != null && Client.ControlService != null)
                    {
                        try
                        {
                            Client.ControlService.Terminate(slog2);
                        }
                        catch (Exception ex)
                        {
                            TraceLog.WriteException(ex, "Unable to terminate slog2info");
                        }
                    }

                    slog2.Dispose();
                    disposed = true;
                }

                if (Client != null)
                {
                    if (Client.ConsoleLogService != null)
                    {
                        Client.ConsoleLogService.Captured -= OnRemoteLogsCaptured;
                        disposed = true;
                    }
                }

                return disposed;
            }

            private void InitializeLogServices()
            {
                // setup console logging:
                if (Client != null && Client.ConsoleLogService != null)
                {
                    Client.ConsoleLogService.Captured -= OnRemoteLogsCaptured;
                    Client.ConsoleLogService.Captured += OnRemoteLogsCaptured;
                }

                // setup slog2info monitor to grab all the logs from the device:
                if (_startingSLog2)
                {
                    return;
                }

                try
                {
                    _startingSLog2 = true;
                    if (Client != null && Client.LauncherService != null && SLog2Info == null)
                    {
                        try
                        {
                            SLog2Info = Client.LauncherService.Start<TargetProcessSLog2Info>("/bin/slog2info", new[] { "-n", "-W", "-s" });
                            TraceLog.WriteLine("Started slog2info with PID: {0}", SLog2Info.PID);
                        }
                        catch (Exception ex)
                        {
                            SLog2Info = null;

                            // probably PlayBook...
                            TraceLog.WriteException(ex, "Failed to start slog2info remotely");
                        }

                        if (SLog2Info != null)
                        {
                            SLog2Info.Captured += OnRemoteLogsCaptured;
                            SLog2Info.Finished += OnSLog2InfoFinished;
                        }
                    }
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to launch slog2info to monitor all activities on the device");
                }
                finally
                {
                    _startingSLog2 = false;
                }
            }

            private void OnSLog2InfoFinished(object sender, EventArgs e)
            {
                TraceLog.WriteLine("Terminated slog2info");

                var slog2 = SLog2Info;
                SLog2Info = null;

                if (slog2 != null)
                {
                    slog2.Captured -= OnRemoteLogsCaptured;
                    slog2.Finished -= OnSLog2InfoFinished;
                }
            }

            private void OnRemoteLogsCaptured(object sender, CapturedLogsEventArgs e)
            {
                foreach (var log in e.Entries)
                {
                    // print each console log and slog2 which matches current settings:
                    if (log.Type == TargetLogEntry.LogType.Console || _optionSLog2Filter(Client.ConsoleLogService, log))
                    {
                        PrintLog(log);
                    }
                }
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

            /// <summary>
            /// Method to verify internal state, when another connection is supposed to be opened to this target.
            /// </summary>
            public void NewConnectionRequested()
            {
                InitializeLogServices();
            }
        }

        #endregion

        private static readonly List<TargetInfo> _activeTargets;
        private static readonly object _sync;

        private static bool _optionInjectLogs;
        private static bool _optionAcceptTracingDebuggedOnly;
        private static uint _optionLogInterval;
        private static LogPredicate _optionSLog2Filter;
        private static LogFormatter _optionSLog2Formatter;
        private static string[] _optionSLog2BufferSets;
        private static string _optionCategoryInject;

        static Targets()
        {
            _activeTargets = new List<TargetInfo>();
            _sync = new object();

            _optionInjectLogs = true;
            _optionAcceptTracingDebuggedOnly = false;
            _optionLogInterval = 0;
            _optionSLog2Filter = SLog2FilterDefault;
            _optionSLog2Formatter = SLog2FormatterDefault;
            _optionCategoryInject = TraceLog.CategoryDevice;
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
        /// Gets an indication, if there is already a valid connection to the device.
        /// </summary>
        public static bool IsConnected(DeviceDefinition device)
        {
            return device != null && IsConnected(device.IP);
        }

        /// <summary>
        /// Gets an indication, if there is trying to connect to the device with given IP.
        /// </summary>
        public static bool IsConnecting(string ip)
        {
            var connection = Find(ip);
            return connection != null && connection.Status == TargetStatus.Connecting;
        }

        /// <summary>
        /// Gets an indication, if there is trying to connect to the device.
        /// </summary>
        public static bool IsConnecting(DeviceDefinition device)
        {
            return device != null && IsConnecting(device.IP);
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
        public static QConnClient Get(DeviceDefinition device, string sshPublicKeyPath, string sshPrivateKeyPath)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(sshPublicKeyPath))
                throw new ArgumentNullException("sshPublicKeyPath");
            if (string.IsNullOrEmpty(sshPrivateKeyPath))
                throw new ArgumentNullException("sshPrivateKeyPath");

            var qClient = Get(device.IP);

            // need to connect first:
            if (qClient == null)
            {
                Connect(device, sshPublicKeyPath, sshPrivateKeyPath, null);

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
        public static void Connect(string ip, string password, DeviceDefinitionType type, string sshPublicKeyPath, string sshPrivateKeyPath, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            Connect(new DeviceDefinition("Ad-hoc device", ip, password, type), sshPublicKeyPath, sshPrivateKeyPath, resultHandler);
        }

        /// <summary>
        /// Requests secure connection setup to given device.
        /// </summary>
        public static void Connect(DeviceDefinition device, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            Connect(device, ConfigDefaults.SshPublicKeyPath, ConfigDefaults.SshPrivateKeyPath, resultHandler);
        }

        /// <summary>
        /// Requests secure connection setup to given device.
        /// </summary>
        public static void Connect(DeviceDefinition device, string sshPublicKeyPath, string sshPrivateKeyPath, EventHandler<TargetConnectionEventArgs> resultHandler)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (string.IsNullOrEmpty(sshPublicKeyPath))
                throw new ArgumentNullException("sshPublicKeyPath");
            if (string.IsNullOrEmpty(sshPrivateKeyPath))
                throw new ArgumentNullException("sshPrivateKeyPath");

            TargetInfo existingTarget;
            lock (_sync)
            {
                existingTarget = NoSyncFind(device.IP);

                // this trick will let only one thread add new connection-info, if both blocked on the sync above:
                if (existingTarget == null)
                {
                    var newTarget = new TargetInfo(device, sshPublicKeyPath, sshPrivateKeyPath);
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
                    existingTarget.NewConnectionRequested();

                    if (resultHandler != null)
                    {
                        existingTarget.StatusChanged += resultHandler;
                    }
                }
            }

            // check if already connected:
            if (existingTarget != null)
            {
                // and notify the caller (it was already mounted to monitor the future status changes):
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
                throw new ArgumentNullException("target");

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

        /// <summary>
        /// Forces disconnection for all kwnon devices that are *outside* of this list.
        /// </summary>
        public static void DisconnectIfOutside(DeviceDefinition[] devices)
        {
            if (devices != null)
            {
                var toDisconnect = new List<TargetInfo>();
                lock (_sync)
                {
                    foreach (var target in _activeTargets)
                    {
                        if (!DeviceDefinition.ContainsWithIP(devices, target.Device.IP))
                        {
                            toDisconnect.Add(target);
                        }
                    }
                }

                // and now... disconnect them:
                foreach (var target in toDisconnect)
                {
                    Disconnect(target.Device);
                }
            }
        }

        /// <summary>
        /// Starts tracing console output for specified process on a target device.
        /// It will fail, if not connected to that device before.
        /// </summary>
        public static bool Trace(string ip, ProcessInfo process, bool isDebugging)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (process == null)
                throw new ArgumentNullException("process");

            var qClient = Get(ip);
            if (qClient == null || qClient.FileService == null || qClient.ConsoleLogService == null)
            {
                TraceLog.WarnLine("Unable to start tracing console outputs for: {0}:\"{1}\"", process.ID, process.ExecutablePath);
                return false;
            }

            if (_optionAcceptTracingDebuggedOnly && !isDebugging)
            {
                TraceLog.WarnLine("Tracing console outputs for non-debugged applications is disabled, change it in Options or manually start capturing logs in Target Navigator ({0}:\"{1}\")",
                    process.ID, process.ExecutablePath);
                return false;
            }

            // start monitoring for console logs:
            return qClient.ConsoleLogService.Start(process, _optionLogInterval, isDebugging);
        }

        /// <summary>
        /// Starts tracing console output for specified process on a target device.
        /// It will fail, if not connected to that device before.
        /// </summary>
        public static bool Trace(DeviceDefinition device, ProcessInfo process, bool isDebugging)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (process == null)
                throw new ArgumentNullException("process");

            return Trace(device.IP, process, isDebugging);
        }

        /// <summary>
        /// Stops all tracing.
        /// </summary>
        public static int TraceStop()
        {
            TargetInfo[] activeTargets;

            lock (_sync)
            {
                activeTargets = _activeTargets.ToArray();
            }

            int result = 0;

            if (activeTargets.Length > 0)
            {
                foreach (var target in activeTargets)
                {
                    if (target.StopLogServices())
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Stops tracing console output from all processes on a given target device.
        /// </summary>
        public static bool TraceStop(DeviceDefinition device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            return TraceStop(device.IP);
        }

        /// <summary>
        /// Stops tracing console output from specified process on a given target device.
        /// </summary>
        public static bool TraceStop(DeviceDefinition device, ProcessInfo process)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (process == null)
                throw new ArgumentNullException("process");

            return TraceStop(device.IP, process);
        }

        /// <summary>
        /// Stops tracing console output from all processes on a given target device.
        /// </summary>
        public static bool TraceStop(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");

            var qClient = Get(ip);
            if (qClient != null && qClient.ConsoleLogService != null)
            {
                try
                {
                    qClient.ConsoleLogService.StopAll();
                    return true;
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to stop console log monitors for: {0}", qClient.Name);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Stops tracing console outputs from specified process on a given target device.
        /// </summary>
        public static bool TraceStop(string ip, ProcessInfo process)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (process == null)
                throw new ArgumentNullException("process");

            var qClient = Get(ip);
            if (qClient != null && qClient.ConsoleLogService != null)
            {
                try
                {
                    return qClient.ConsoleLogService.Stop(process);
                }
                catch (Exception ex)
                {
                    TraceLog.WriteException(ex, "Unable to stop console log monitors for: {0}", qClient.Name);
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks, whether specified process' console output is traced.
        /// </summary>
        public static bool TraceIs(DeviceDefinition device, ProcessInfo process)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            if (process == null)
                throw new ArgumentNullException("process");

            return TraceIs(device.IP, process);
        }

        /// <summary>
        /// Checks, whether specified process' console output is traced.
        /// </summary>
        public static bool TraceIs(string ip, ProcessInfo process)
        {
            if (string.IsNullOrEmpty(ip))
                throw new ArgumentNullException("ip");
            if (process == null)
                throw new ArgumentNullException("process");

            var qClient = Get(ip);
            if (qClient != null && qClient.ConsoleLogService != null)
            {
                return qClient.ConsoleLogService.IsMonitoring(process);
            }

            return false;
        }

        /// <summary>
        /// Sets the logging options.
        /// </summary>
        public static void TraceOptions(bool debuggedOnly, uint logsInterval, int slog2Level, int slog2FormatterLevel, string[] slog2BufferSets, bool injectLogs, string injectCategory)
        {
            _optionInjectLogs = injectLogs;
            _optionAcceptTracingDebuggedOnly = debuggedOnly;
            _optionLogInterval = logsInterval;
            _optionSLog2BufferSets = slog2BufferSets;

            if (!string.IsNullOrEmpty(injectCategory))
            {
                _optionCategoryInject = injectCategory;
            }

            switch (slog2Level)
            {
                case 0: // nothing should be monitored
                    _optionSLog2Filter = SLog2FilterNone;
                    break;
                case 1: // only developed applications
                    if (slog2BufferSets == null || slog2BufferSets.Length == 0)
                    {
                        _optionSLog2Filter = SLog2FilterApps;
                    }
                    else
                    {
                        _optionSLog2Filter = SLog2FilterAppsWithBufferSet;
                    }
                    break;
                case 2: // whole system
                    _optionSLog2Filter = SLog2FilterSystem;
                    break;
                default: // restore default, if out-of-range
                    _optionSLog2Filter = SLog2FilterDefault;
                    break;
            }

            switch (slog2FormatterLevel)
            {
                case 0: // default:
                    _optionSLog2Formatter = SLog2FormatterDefault;
                    break;
                case 1: // prefixed with title:
                    _optionSLog2Formatter = SLog2FormatterTildeMessage;
                    break;
                case 2: // prefixed with hash:
                    _optionSLog2Formatter = SLog2FormatterHashMessage;
                    break;
                case 3: // prefixed with PID:
                    _optionSLog2Formatter = SLog2FormatterPidMessage;
                    break;
                case 4: // prefixed with appID:
                    _optionSLog2Formatter = SLog2FormatterAppIdMessage;
                    break;
                case 5: // prefixed with buffer-set:
                    _optionSLog2Formatter = SLog2FormatterBufferSetMessage;
                    break;
                case 6: // prefixed with appID and buffer-set:
                    _optionSLog2Formatter = SLog2FormatterAppIdBufferSetMessage;
                    break;
                case 7: // prefixed with PID and buffer-set:
                    _optionSLog2Formatter = SLog2FormatterPidBufferSetMessage;
                    break;
                case 8: // prefixed with PID and appID:
                    _optionSLog2Formatter = SLog2FormatterPidAppIdMessage;
                    break;
                default: // restore default, if out-of-range
                    _optionSLog2Formatter = SLog2FormatterDefault;
                    break;
            }

            // and update the console log interval for already running loggers:
            lock (_sync)
            {
                foreach (var target in _activeTargets)
                {
                    if (target.Client != null && target.Client.ConsoleLogService != null)
                    {
                        target.Client.ConsoleLogService.Interval = logsInterval;
                    }
                }
            }
        }

        /// <summary>
        /// Writes the log with current formatting settings into the output.
        /// </summary>
        private static void PrintLog(TargetLogEntry log)
        {
            if (log.Type == TargetLogEntry.LogType.SLog2)
            {
                PrintLogMessage(_optionSLog2Formatter(log));
            }
            else
            {
                PrintLogMessage(log.Message);
            }
        }

        private static void PrintLogMessage(string message)
        {
            TraceLog.WriteLine(message);

            // print logs on default 'Debug' output window, if allowed:
            if (_optionInjectLogs)
            {
                System.Diagnostics.Trace.WriteLine(message, _optionCategoryInject);
            }
        }

        #region slog2 Formatter

        private static string SLog2FormatterDefault(TargetLogEntry log)
        {
            return log.Message;
        }

        private static string SLog2FormatterTildeMessage(TargetLogEntry log)
        {
            return string.Concat("~ ", log.Message);
        }

        private static string SLog2FormatterHashMessage(TargetLogEntry log)
        {
            return string.Concat("# ", log.Message);
        }

        private static string SLog2FormatterPidMessage(TargetLogEntry log)
        {
            return string.Concat(log.PID, ": ", log.Message);
        }

        private static string SLog2FormatterAppIdMessage(TargetLogEntry log)
        {
            return string.Concat(log.AppID, ": ", log.Message);
        }

        private static string SLog2FormatterBufferSetMessage(TargetLogEntry log)
        {
            return string.Concat(log.BufferSet, ": ", log.Message);
        }

        private static string SLog2FormatterAppIdBufferSetMessage(TargetLogEntry log)
        {
            return string.Concat(log.AppID, "::", log.BufferSet, ": ", log.Message);
        }

        private static string SLog2FormatterPidBufferSetMessage(TargetLogEntry log)
        {
            return string.Concat(log.PID, "::", log.BufferSet, ": ", log.Message);
        }

        private static string SLog2FormatterPidAppIdMessage(TargetLogEntry log)
        {
            return string.Concat(log.PID, "::", log.AppID, ": ", log.Message);
        }

        #endregion

        #region slog2 Filters

        private static bool IsMonitoring(TargetServiceConsoleLog service, uint pid)
        {
            return !service.IsMonitoringAnything || service.IsMonitoring(pid);
        }

        private static bool SLog2FilterDefault(TargetServiceConsoleLog service, TargetLogEntry log)
        {
            return log.PID == 0 || string.IsNullOrEmpty(log.BufferSet) || (IsMonitoring(service, log.PID) && log.BufferSet == "default");
        }

        private static bool SLog2FilterNone(TargetServiceConsoleLog service, TargetLogEntry log)
        {
            return false;
        }

        private static bool SLog2FilterSystem(TargetServiceConsoleLog service, TargetLogEntry log)
        {
            return true;
        }

        private static bool SLog2FilterApps(TargetServiceConsoleLog service, TargetLogEntry log)
        {
            return log.PID == 0 || IsMonitoring(service, log.PID);
        }

        private static bool SLog2FilterAppsWithBufferSet(TargetServiceConsoleLog service, TargetLogEntry log)
        {
            return log.PID == 0 || (IsMonitoring(service, log.PID) && InBufferSet(log.BufferSet));
        }

        private static bool InBufferSet(string name)
        {
            if (_optionSLog2BufferSets == null || string.IsNullOrEmpty(name))
                return true;

            foreach (var bufferName in _optionSLog2BufferSets)
            {
                if (string.Compare(bufferName, name, StringComparison.Ordinal) == 0)
                    return true;
            }

            return false;
        }

        #endregion
    }
}
