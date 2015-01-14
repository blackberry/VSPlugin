using System;
using System.Threading;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing information about manually started process.
    /// </summary>
    public class TargetProcess : IDisposable
    {
        private TargetServiceLauncher _service;
        private QConnConnection _connection;
        private QDataSource _dataSource;
        private bool _isSuspended;
        private Thread _monitoringThread;

        public event EventHandler<EventArgs> Finished;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetProcess(TargetServiceLauncher service, QConnConnection connection, uint pid, bool suspended)
        {
            if (service == null)
                throw new ArgumentNullException("service");
            if (connection == null)
                throw new ArgumentNullException("connection");

            _service = service;
            _connection = connection;
            PID = pid;
            _isSuspended = suspended;
            _monitoringThread = new Thread(MonitorProcessOutputs);

            StartOutputMonitoring();
        }

        ~TargetProcess()
        {
            Dispose(false);
        }

        #region Properties

        /// <summary>
        /// Checks, whether the specified command is suspended and expects Continue() to be called.
        /// </summary>
        public bool IsSuspended
        {
            get { return !_isSuspended; }
        }

        /// <summary>
        /// Gets an indication, if process already finished or connection was lost.
        /// </summary>
        public bool IsFinished
        {
            get { return _monitoringThread == null; }
        }

        /// <summary>
        /// Gets the ID of the process on target device.
        /// </summary>
        public uint PID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the exit code for this process.
        /// </summary>
        public uint ExitCode
        {
            get { return _service != null ? _service.GetExitCode(this) : uint.MaxValue; }
        }

        #endregion

        /// <summary>
        /// Resumes suspended process.
        /// </summary>
        public void Continue()
        {
            if (!_isSuspended)
                throw new QConnException("Process with ID: " + PID + " is not suspended");

            _connection.Send("continue");
            _isSuspended = false;

            StartOutputMonitoring();
        }

        /// <summary>
        /// Waits until the process finishes.
        /// </summary>
        public void Join()
        {
            if (_monitoringThread != null)
            {
                _monitoringThread.Join();
            }
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposing used resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _service = null;
                if (_connection != null)
                {
                    _connection.Dispose();
                    _connection = null;
                }

                if (_dataSource != null)
                {
                    _dataSource.Dispose();
                    _dataSource = null;
                }

                if (_monitoringThread != null)
                {
                    _monitoringThread.Join();
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private void StartOutputMonitoring()
        {
            // ignore this method, if process is suspended:
            if (_isSuspended)
                return;

            _dataSource = _connection.DetachDataSource();
            _dataSource.ReceiveTimeout = Timeout.Infinite;
            _dataSource.SendTimeout = Timeout.Infinite;
            _connection = null; // won't be needed anymore and was already disposed by the detach call

            _monitoringThread.Start();
        }

        private void MonitorProcessOutputs()
        {
            try
            {
                // wait until the process is started:
                while (_isSuspended)
                {
                    Thread.Sleep(500);
                }

                // and then monitor its outputs:
                ReadOutputs();
            }
            catch (ThreadAbortException)
            {
            }

            // done...
            _monitoringThread = null;

            if (Finished != null)
            {
                Finished(this, EventArgs.Empty);
            }

            Dispose();
        }

        /// <summary>
        /// Reads raw binary data from running processes output-stream.
        /// </summary>
        private void ReadOutputs()
        {
            ProcessOutputInitialize();

            while (true)
            {
                if (_dataSource == null)
                {
                    QTraceLog.WriteLine("!! Disposed data-source encountered");
                    return;
                }

                byte[] data;
                var result = _dataSource.Receive(int.MaxValue, out data);
                switch (result)
                {
                    case HResult.OK:

                        if (data != null && data.Length > 0)
                        {
                            // new data arrived, process it somehow:
                            if (ProcessOutputData(result, data))
                            {
                                ProcessOutputDone();
                                return;
                            }
                        }
                        break;
                    default:
                        QTraceLog.WriteLine("Finishing monitoring remote process outputs, result: " + result);
                        ProcessOutputDone();
                        return;
                }
            }
        }

        #region Virtual Methods

        /// <summary>
        /// Method called once, when preparing to receive running processes output stream.
        /// </summary>
        protected virtual void ProcessOutputInitialize()
        {
        }

        /// <summary>
        /// Method called in a loop to perform actions over received output data.
        /// </summary>
        protected virtual bool ProcessOutputData(HResult result, byte[] data)
        {
            return false;
        }

        /// <summary>
        /// Method called once, when cleaning up after running process terminated and no output data will be delivered.
        /// </summary>
        protected virtual void ProcessOutputDone()
        {
        }

        #endregion
    }
}
