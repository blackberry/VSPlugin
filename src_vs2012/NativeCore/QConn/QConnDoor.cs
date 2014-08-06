using System;
using BlackBerry.NativeCore.Diagnostics;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Requests;
using BlackBerry.NativeCore.QConn.Response;

namespace BlackBerry.NativeCore.QConn
{
    public sealed class QConnDoor
    {
        /// <summary>
        /// Default port the service is operating on the device.
        /// </summary>
        public const int DefaultPort = 4455;
        private const int DefaultResponseSize = 255;

        private readonly QDataSource _source;

        public QConnDoor()
        {
            _source = new QDataSource();
        }

        /// <summary>
        /// Initiates connection with specified target.
        /// </summary>
        public void Connect(string host, int port)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");

            var result = _source.Connect(host, port);
            if (result != HResult.OK)
            {
                throw new SecureTargetConnectionException(result, string.Concat("Unable to connect to target ", host, ":", port));
            }

            // try to initialize communication:
            Send(new SecureTargetHello());
            var response = Receive();
            VerifyResponse(response);
        }

        /// <summary>
        /// Closes connection with current target.
        /// </summary>
        public void Close()
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");
            if (!_source.IsConnected)
            {
                return;
            }

            // request a close of the connection on target:
            Send(new SecureTargetClose());
            var response = Receive();
            VerifyResponse(response);

            // close connection:
            var result = _source.Close();
            if (result != HResult.OK)
                throw new SecureTargetConnectionException(HResult.Fail, "Unable to close connection with target");
        }

        private void Send(SecureTargetRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");

            _source.Send(request.GetData());
        }

        private SecureTargetResult Receive()
        {
            return Receive(new byte[DefaultResponseSize]);
        }

        private SecureTargetResult Receive(byte[] buffer)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            byte[] result;
            var status = _source.Receive(buffer, out result);

            if (result != null && result.Length > 0)
            {
                ushort packetLength = BitHelper.GetUInt16(result, 0);

                if (packetLength > buffer.Length)
                {
                    QTraceLog.WriteLine("Packet length larger than buffer, expected size: " + packetLength);
                    return new SecureTargetResult(result, HResult.BufferTooSmall);
                }
            }

            // interpret response:
            return SecureTargetResult.Load(result, status);
        }

        /// <summary>
        /// Verifies correctness of the response and throws appropriate exception if needed.
        /// </summary>
        private void VerifyResponse(SecureTargetResult result)
        {
            // received anything?
            if (result == null)
            {
                throw new SecureTargetConnectionException(HResult.Abort, "No response received in expected time");
            }

            // received correctly formatted response?
            if (result.Status != HResult.OK)
            {
                // unknown response?
                var response = result as SecureTargetResponse;
                if (response != null && result.Status == HResult.InvalidFrameCode)
                {
                    throw new SecureTargetConnectionException(result.Status, "The target returned an improper response code: " + response.Code);
                }

                throw new SecureTargetConnectionException(result.Status, "A network error occurred while communicating with the target.");
            }

            // does the protocol version match?
            var versionMismatch = result as SecureTargetFeedbackMismatchedVersion;
            if (versionMismatch != null)
            {
                throw new SecureTargetConnectionException(result.Status, versionMismatch.Message);
            }

            // request was rejected?
            var rejected = result as SecureTargetFeedbackRejected;
            if (rejected != null)
            {
                throw new SecureTargetConnectionException(result.Status, "Connection refused: " + rejected.Reason);
            }

            // all was OK... finally!
        }
    }
}
