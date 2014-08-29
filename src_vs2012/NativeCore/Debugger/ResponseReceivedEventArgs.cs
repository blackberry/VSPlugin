using System;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Arguments broadcased along with response received by given components.
    /// </summary>
    public class ResponseReceivedEventArgs : EventArgs
    {
        private bool _handled;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ResponseReceivedEventArgs(Request request, Response response, bool handled)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            Request = request;
            Response = response;
            _handled = handled;
        }

        #region Properties

        /// <summary>
        /// Optional request, that was supposed to issue the response.
        /// </summary>
        public Request Request
        {
            get;
            private set;
        }

        /// <summary>
        /// Received response.
        /// </summary>
        public Response Response
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the indication, if given response was handled.
        /// Once set, it can not be reverted.
        /// </summary>
        public bool Handled
        {
            get { return _handled; }
            set
            {
                if (value)
                    _handled = true;
            }
        }

        #endregion
    }
}