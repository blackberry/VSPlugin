using System;
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

        public void Open(string host, int port)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");

            var result = _source.Connect(host, port);
            if (result != HResult.OK)
                throw new Exception("Unable to connect to target");

            var buffer = new byte[DefaultResponseSize];

            // try to initialize communication:
            Send(new SecureTargetHello());
            var response = Receive(buffer);
        }

        private void Send(SecureTargetRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");

            _source.Send(request.GetData());
        }

        private SecureTargetResult Receive(byte[] buffer)
        {
            if (_source == null)
                throw new ObjectDisposedException("QConnDoor");
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            byte[] result;
            var status = _source.Receive(buffer, out result);

            // interpret response:
            return SecureTargetResult.Load(result, status);
        }
    }
}
