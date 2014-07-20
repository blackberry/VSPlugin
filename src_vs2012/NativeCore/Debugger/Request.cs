using System;
using System.Threading;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class describing a single request sent to the GDB.
    /// </summary>
    public class Request : IDisposable
    {
        private static int _globalID = 1000;


        private readonly uint _id;
        private string _command;
        private AutoResetEvent _event;

        /// <summary>
        /// Event fired each time a response is received.
        /// </summary>
        public event EventHandler<ResponseReceivedEventArgs> Received;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public Request(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            _command = command;
            _event = new AutoResetEvent(false);

            // make sure IDs never repeats, even if request created from different threads:
            _id = (uint)Interlocked.Add(ref _globalID, 1);
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public Request(uint id, string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            _command = string.Concat(id.ToString("D2"), command);
            _event = new AutoResetEvent(false);

            _id = id;
        }

        /// <summary>
        /// Dedicated constructor for a group of requests.
        /// </summary>
        protected Request()
        {
            _event = new AutoResetEvent(false);
        }

        ~Request()
        {
            Dispose(false);
        }

        #region Properties

        public virtual uint ID
        {
            get { return _id; }
        }

        public virtual string Command
        {
            get { return _command; }
            protected set { _command = value; }
        }

        public virtual Response Response
        {
            get;
            private set;
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_event != null)
                {
                    _event.Dispose();
                    _event = null;
                }
            }
        }

        #endregion

        public override string ToString()
        {
            // just send the RAW command
            return _command;
        }

        /// <summary>
        /// Checks if the request has identical ID as specified one.
        /// </summary>
        public bool HasIdenticalID(uint id)
        {
            return ID == id;
        }

        /// <summary>
        /// Method executed to perform request's action. It should return 'true', when all succeeded.
        /// </summary>
        public virtual bool Execute(IGdbSender sender)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");

            return sender.Send(ToString());
        }

        /// <summary>
        /// Mostly for parser testing.
        /// </summary>
        public bool Complete(Response response)
        {
            bool retry;
            return Complete(response, out retry);
        }

        /// <summary>
        /// Method executed by GdbParser, to inform the request, that response has arrived.
        /// There is no limitation for, how many responses a request can handle.
        /// It should just return 'true', when it is the final one and use the 'retry'
        /// parameter to be sent again to GDB.
        /// </summary>
        public virtual bool Complete(Response response, out bool retry)
        {
            if (response == null)
                throw new ArgumentNullException("response");
            if (_event == null)
                throw new ObjectDisposedException("Request");

            // reset the state:
            retry = false;
            Response = response;
            ProcessResponse(response);

            // notify all listeners synchronously:
            var receivedHandler = Received;
            if (receivedHandler != null)
                receivedHandler(this, new ResponseReceivedEventArgs(response, false));

            // wake up one waiting for response thread:
            _event.Set();
            return true;
        }

        /// <summary>
        /// Method executed internally by the request to process incoming data.
        /// </summary>
        protected virtual void ProcessResponse(Response response)
        {
        }

        protected void SetEvent()
        {
            if (_event == null)
                throw new ObjectDisposedException("Request");

            _event.Set();
        }

        /// <summary>
        /// Waits until this request received a response.
        /// </summary>
        /// <returns>Returns 'true', if the signal was received and response was valid, 'false' in case of timeout.</returns>
        public bool Wait()
        {
            return Wait(GdbProcessor.ShortInfinite);
        }

        /// <summary>
        /// Waits specified number of milliseconds until this request received a response.
        /// </summary>
        /// <returns>Returns 'true', if the signal was received and response was valid, 'false' in case of timeout.</returns>
        public bool Wait(int millisecondsTimeout)
        {
            if (_event == null)
                throw new ObjectDisposedException("Request");

            return _event.WaitOne(millisecondsTimeout);
        }
    }
}
