using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing information about manually started process.
    /// </summary>
    public sealed class TargetProcess : IDisposable
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

        private void Dispose(bool disposing)
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
        }

        private void ReadOutputs()
        {
            byte[] data;
            byte[] lastLine = null;

            while (true)
            {
                if (_dataSource == null)
                {
                    QTraceLog.WriteLine("!! Disposed data-source encountered");
                    return;
                }

                var result = _dataSource.Receive(int.MaxValue, out data);
                switch (result)
                {
                    case HResult.OK:
                        // new data arrived, split it into lines of text:
                        var logEntries = ReadLines(data, ref lastLine);

                        // and print:
                        if (logEntries != null)
                        {
                            foreach (var message in logEntries)
                            {
                                TraceLog.WriteLine(message);
                                Trace.WriteLine(message, TraceLog.CategoryDevice);
                            }
                        }
                        break;
                    default:
                        QTraceLog.WriteLine("Finishing monitoring remote process outputs, result: " + result);
                        return;
                }
            }
        }

        private string[] ReadLines(byte[] data, ref byte[] lastLine)
        {
            if (data == null || data.Length == 0)
                return null;

            var result = new List<string>();
            int lineStart = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 10) // '\n'
                {
                    // is it the first line?
                    if (lineStart < 0)
                    {
                        AddString(result, GetString(lastLine, data, lineStart + 1, i));
                        lastLine = null;
                    }
                    else
                    {
                        AddString(result, GetString(null, data, lineStart + 1, i - lineStart - 1));
                    }

                    // move the end of line marker:
                    lineStart = i + 1 < data.Length && data[i + 1] == 13 ? i + 1 : i;
                }
            }

            // the end of the message is not a properly finished new line:
            if (lineStart < 0 || lineStart != data.Length - 1)
            {
                lastLine = BitHelper.Combine(lastLine, data, lineStart + 1, data.Length - lineStart - 1);
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        private void AddString(List<string> result, string message)
        {
            if (message == null)
                return;

            if (result.Count == 0 || string.Compare(result[result.Count - 1], message, StringComparison.Ordinal) != 0)
            {
                result.Add(message);
            }
        }

        private string GetString(byte[] lineStart, byte[] data, int dataFrom, int dataLength)
        {
            return Encoding.UTF8.GetString(BitHelper.Combine(lineStart, data, dataFrom, dataLength));
        }
    }
}
