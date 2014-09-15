using System;
using System.Collections.Generic;
using System.Net.Sockets;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn
{
    /// <summary>
    /// Class transmitting data to and back from QNX services on the device.
    /// </summary>
    public sealed class QDataSource : IDisposable
    {
        private const int Timeout = 10000;
        private const int DefaultChunkSize = 512;

        private Socket _socket;
        private byte[] _buffer;
        private readonly int _chunkSize;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public QDataSource()
        {
            _chunkSize = DefaultChunkSize;
        }

        /// <summary>
        /// Init constructor.
        /// Defines the chunk size, data from socket is read.
        /// </summary>
        public QDataSource(int chunkSize)
        {
            if (chunkSize < 0)
                throw new ArgumentOutOfRangeException("chunkSize");
            _chunkSize = chunkSize == 0 ? DefaultChunkSize : chunkSize;
        }

        ~QDataSource()
        {
            Dispose(false);
        }

        #region Properties

        public bool IsConnected
        {
            get { return _socket != null && _socket.Connected; }
        }

        /// <summary>
        /// Gets the size of the data read chunk.
        /// </summary>
        public int ChunkSize
        {
            get { return _chunkSize; }
        }

        /// <summary>
        /// Gets or sets the sending data timeout.
        /// </summary>
        public int SendTimeout
        {
            get { return _socket != null ? _socket.SendTimeout : 0; }
            set
            {
                if (_socket != null)
                {
                    _socket.SendTimeout = value <= 0 ? Timeout : value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the receiving data timeout.
        /// </summary>
        public int ReceiveTimeout 
        {
            get { return _socket != null ? _socket.ReceiveTimeout : 0; }
            set
            {
                if (_socket != null)
                {
                    _socket.ReceiveTimeout = value <= 0 ? Timeout : value;
                }
            }
        }

        #endregion

        /// <summary>
        /// Establish data connection with a target.
        /// </summary>
        public HResult Connect(string host, int port, int timeout)
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
                    _socket.ReceiveTimeout = Timeout;
                    _socket.DontFragment = true;
                    _socket.NoDelay = true; // don't combine small TCP packets into one, just send them immediately as data to transmit was given

                    // don't want to use synchronous version of _socket.Connect(host, port) as this will block for undefined amount of seconds
                    //_socket.Connect(host, port);

                    if (timeout <= 0)
                    {
                        timeout = Timeout;
                    }

                    var asyncResult = _socket.BeginConnect(host, port, null, null);
                    var success = asyncResult.AsyncWaitHandle.WaitOne(timeout, true);

                    if (!success)
                    {
                        _socket.Dispose();
                        QTraceLog.WriteLine("Unable to establish connection to: {0}:{1} with timeout {2} ms", host, port, timeout);
                        return HResult.Fail;
                    }

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

            // allocate new temp buffer for data:
            if (_buffer == null)
            {
                _buffer = new byte[_chunkSize];
            }

            try
            {
                // remove any available data from 'previous' requests:
                while (_socket.Available > 0)
                {
                    _socket.Receive(_buffer);
                }

                // issue new request:
                int sent = _socket.Send(data);
                if (sent < 0)
                {
                    QTraceLog.WriteLine("Unable to send data");
                    return HResult.Fail;
                }
                if (sent != data.Length)
                {
                    QTraceLog.WriteLine("Invalid number of bytes send ({0} instead of {1})", sent, data.Length);
                    return HResult.Fail;
                }
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
        /// Reads specified amount of available data from target.
        /// MaxLength parameter serves only as a suggestion here. It might happen,
        /// that result will be smaller than maxLength, but for sure, it won't return a bigger buffer.
        /// 
        /// That's why there are two possible usages:
        ///  - to call this method with int.MaxValue to just read *all* available data
        ///  - or with small value, to only get, what is currently needed for processing and leave the rest for next Receive() request.
        /// </summary>
        public HResult Receive(int maxLength, out byte[] result)
        {
            if (!IsConnected)
            {
                QTraceLog.WriteLine("Attempt to read data from closed source");
                result = new byte[0];
                return HResult.Fail;
            }

            // allocate new temp buffer for data:
            if (_buffer == null)
            {
                _buffer = new byte[_chunkSize];
            }

            // PH: hint:
            // The idea of following code is to try at least once to perform a read of the data from the socket.
            // Then it will loop and simply stop, when there is nothing more available.
            //
            // There are few optimizations implemented. Reading is done in chunks. If somehow the expected maxLength
            // equals the chunk size, the local array will be returned (to avoid copying) and forget.
            // In any other case, the result data is a new array filled with data copied from buffer and previously read chunks.
            try
            {
                int totalLength = 0;
                int length;
                List<byte[]> extraBytes = null;

                while (true)
                {
                    length = _socket.Receive(_buffer, 0, _buffer.Length, SocketFlags.None);

                    // reading failed?
                    if (length < 0)
                    {
                        break;
                    }

                    totalLength += length;

                    // read enough or whole data?
                    if (totalLength >= maxLength || _buffer.Length != length || _socket.Available == 0)
                    {
                        break;
                    }

                    // store read data and alloc new buffer for next chunk:
                    if (extraBytes == null)
                    {
                        extraBytes = new List<byte[]>();
                    }
                    extraBytes.Add(_buffer);
                    _buffer = new byte[_chunkSize];
                }

                // read the data correctly?
                if (length <= 0)
                {
                    if (extraBytes == null)
                    {
                        QTraceLog.WriteLine("Unable to read data");
                        result = new byte[0];
                        return HResult.Fail;
                    }

                    // last buffer reading failed, then combine all other extra bytes:
                    result = BitHelper.Combine(extraBytes, null, 0);
                }
                else
                {
                    // did we read exactly, what was expected?
                    if (length == totalLength && length == _buffer.Length && extraBytes == null)
                    {
                        result = _buffer;
                        _buffer = null;
                    }
                    else
                    {
                        // or just combine all results:
                        result = BitHelper.Combine(extraBytes, _buffer, length);
                    }
                }
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
                _buffer = null;
            }
        }

        #endregion
    }
}
