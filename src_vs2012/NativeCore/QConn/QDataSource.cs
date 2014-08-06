using System;
using System.Net.Sockets;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Class transmitting data to and back from QNX services on the device.
    /// </summary>
    public sealed class QDataSource : IDisposable
    {
        private const int Timeout = 10000;

        private Socket _socket;

        ~QDataSource()
        {
            Dispose(false);
        }

        #region Properties

        public bool IsConnected
        {
            get { return _socket != null && _socket.Connected; }
        }

        #endregion

        /// <summary>
        /// Establish data connection with a target.
        /// </summary>
        public HResult Connect(string host, int port)
        {
            if (string.IsNullOrEmpty(host))
                throw new ArgumentNullException("host");

            bool connected = IsConnected;
            if (!connected)
            {
                try
                {
                    _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _socket.SendTimeout = Timeout;
                    _socket.Connect(host, port);

                    // make sure, the object is listed on the finalizers thread,
                    // in case multiple times connection was opened and closed...
                    GC.ReRegisterForFinalize(this);
                }
                catch (Exception ex)
                {
                    QTraceLog.WriteException(ex, "Unable to establish connection to: {0}:{1}", host, port);
                    return HResult.Fail;
                }
            }

            return HResult.OK;
        }

        /// <summary>
        /// Closes the data connection.
        /// </summary>
        public HResult Close()
        {
            Dispose(true);
            return HResult.OK;
        }

        /// <summary>
        /// Sends data to the target.
        /// </summary>
        public HResult Send(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            if (!IsConnected)
            {
                QTraceLog.WriteLine("Attempt to write to closed source");
                return HResult.Fail;
            }

            try
            {
                _socket.Send(data);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed to send data");
                Close();
                return HResult.Fail;
            }

            return HResult.OK;
        }

        /// <summary>
        /// Reads specified amount of data from target.
        /// </summary>
        public HResult Receive(byte[] buffer, out byte[] result)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (buffer.Length == 0)
                throw new ArgumentOutOfRangeException("buffer");

            if (!IsConnected)
            {
                QTraceLog.WriteLine("Attempt to read data from closed source");
                result = new byte[0];
                return HResult.Fail;
            }

            try
            {
                var length = _socket.Receive(buffer, 0, buffer.Length, SocketFlags.None);

                result = new byte[length];
                Array.Copy(buffer, 0, result, 0, length);
            }
            catch (Exception ex)
            {
                QTraceLog.WriteException(ex, "Failed to read data");
                result = new byte[0];
                return HResult.Fail;
            }

            return HResult.OK;
        }

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Close();
                    }
                    catch (Exception ex)
                    {
                        QTraceLog.WriteException(ex, "Unable to close connection");
                    }
                    _socket = null;
                }
            }
        }

        #endregion
    }
}
