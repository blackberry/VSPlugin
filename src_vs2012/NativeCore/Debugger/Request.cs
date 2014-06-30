using System;
using System.Globalization;
using System.Threading;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class describing a single request sent to the GDB.
    /// </summary>
    public class Request : IDisposable
    {
        private static int _globalID;


        private AutoResetEvent _event;

        public Request(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException("command");

            Command = command;
            _event = new AutoResetEvent(false);

            // make sure IDs never repeats, even if request created from different threads:
            ID = Interlocked.Add(ref _globalID, 1).ToString(CultureInfo.InvariantCulture);
        }

        ~Request()
        {
            Dispose(false);
        }

        #region Properties

        public string ID
        {
            get;
            private set;
        }

        public string Command
        {
            get;
            protected set;
        }

        public Response Response
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
            return string.Concat(ID, "-", Command);
        }

        /// <summary>
        /// Checks if the request has identical ID as specified one.
        /// </summary>
        public bool HasIdenticalID(string id)
        {
            if (ID == null && id == null)
                return true;
            return string.Compare(id, ID, StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Method executed by GdbParser, to inform the request, that response has arrived.
        /// There is no limitation for, how many responses a request can handle.
        /// It should just return 'true', when it is the final one.
        /// </summary>
        internal bool Complete(Response response)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            Response = response;
            _event.Set();
            return true;
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
