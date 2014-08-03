using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using BlackBerry.NativeCore.Tools;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class designed to communited directly with GDB. It is required to startup GDB in MI mode.
    /// More info here: http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI.html
    /// </summary>
    public sealed class GdbProcessor : IDisposable
    {
        public const string Prompt = "(gdb) ";
        internal const int ShortInfinite = 30 * 1000; // 30 sec

        private readonly IGdbSender _sender;
        private readonly object _sync;
        private AutoResetEvent _eventMessageAvailable;

        private readonly List<string> _messageCache;
        private readonly Queue<Response> _responses;

        private Request _currentRequest;
        private readonly Queue<Request> _requests;

        public event EventHandler<ResponseReceivedEventArgs> Received;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public GdbProcessor(IGdbSender sender)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");

            _sender = sender;
            _sync = new object();
            _eventMessageAvailable = new AutoResetEvent(false);

            _messageCache = new List<string>();
            _responses = new Queue<Response>();
            _requests = new Queue<Request>();
            IsClosed = false;
        }

        ~GdbProcessor()
        {
            Dispose(false);
        }

        #region Properties

        public IEventDispatcher Dispatcher
        {
            get;
            set;
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        #endregion

        #region IDisposable Implemenation

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool dispose)
        {
            IsClosed = true;

            if (dispose)
            {
                Received = null;

                if (_eventMessageAvailable != null)
                {
                    _eventMessageAvailable.Dispose();
                    _eventMessageAvailable = null;
                }
            }
        }

        #endregion

        /// <summary>
        /// Stores each line of text received from GDB for further processing and converts them into a complete message.
        /// Single message is a multiline notification, that usually is response to request issued earlier or asynchronous notification.
        /// </summary>
        /// <returns>Returns 'true', when whole new message has been read and there is something to process.</returns>
        public bool Receive(string text)
        {
            // ignore empty lines:
            if (string.IsNullOrEmpty(text))
                return false;

            Request currentRequest;
            Response response = null;
            bool clearCurrentRequest = false;
            bool retryRequest = false;

            lock (_sync)
            {
                currentRequest = _currentRequest;
                var currentID = currentRequest != null ? currentRequest.ID.ToString(CultureInfo.InvariantCulture) : null;
                var currentIDLength = string.IsNullOrEmpty(currentID) ? 0 : currentID.Length;

                // is it an end of the message?
                if (text == Prompt)
                {
                    // then convert to message object and pass for further processing:
                    if (_messageCache.Count > 0)
                    {
                        response = Response.Parse(_messageCache.ToArray());
                        _messageCache.Clear();
                    }
                }
                else
                {
                    // check, if exit response, in that case, don't wait for never coming prompt:
                    if (string.CompareOrdinal(text, currentIDLength, "^exit", 0, text.Length - currentIDLength) == 0)
                    {
                        response = Response.Parse(new[] { text });
                        if (response != null)
                        {
                            IsClosed = true;
                        }
                    }

                    if (response == null)
                    {
                        _messageCache.Add(text);
                    }
                }
            }

            // if completed the message wake up one thread waiting for ANY response:
            if (response != null)
            {
                // inform a request, that a matching response arrived:
                if (currentRequest != null && currentRequest.HasIdenticalID(response.ID))
                {
                    clearCurrentRequest = currentRequest.Complete(response, out retryRequest);
                }

                // if response was not handled by listeners of Received event
                // than add it to the queue for to be processed by waiting thread:
                if (!NotifyResponseReceived(clearCurrentRequest ? currentRequest : null, response))
                {
                    lock (_sync)
                    {
                        _responses.Enqueue(response);
                        _eventMessageAvailable.Set();
                    }
                }
            }

            // send the same request, in case it expects more data
            // or the next stored one:
            if (retryRequest)
            {
                if (!Send(currentRequest))
                    return false;
            }
            else
            {
                if (clearCurrentRequest)
                {
                    _currentRequest = null;
                    if (!SendNextRequest())
                        return false;
                }
            }

            return response != null;
        }

        private bool NotifyResponseReceived(Request request, Response response)
        {
            var receivedHandler = Received;
            var dispatcher = Dispatcher;
            ResponseReceivedEventArgs e = null;

            if (dispatcher != null)
            {
                e = new ResponseReceivedEventArgs(request, response, false);
                dispatcher.Invoke(receivedHandler, this, e); 
            }
            else
            {
                if (receivedHandler != null)
                {
                    e = new ResponseReceivedEventArgs(request, response, false);
                    receivedHandler(this, e);
                }
            }

            return e != null && e.Handled;
        }

        /// <summary>
        /// Waits until a valid message was not received.
        /// </summary>
        /// <returns>Returns 'true', if the signal was received and data is valid, 'false' in case of timeout.</returns>
        public bool Wait(out Response response)
        {
            return Wait(ShortInfinite, out response);
        }

        /// <summary>
        /// Waits until a valid message was not received.
        /// </summary>
        /// <returns>Returns 'true', if the signal was received and data is valid, 'false' in case of timeout.</returns>
        public bool Wait(int millisecondsTimeout, out Response response)
        {
            bool hasSignal = _eventMessageAvailable.WaitOne(millisecondsTimeout);

            if (hasSignal)
            {
                lock (_sync)
                {
                    response = _responses.Dequeue();
                }
            }
            else
            {
                response = null;
            }

            return hasSignal && response != null;
        }

        /// <summary>
        /// Reads next response from the buffer.
        /// If there is nothing received until last call, null value is returned.
        /// </summary>
        public Response Read()
        {
            Response response;

            lock (_sync)
            {
                response = _responses.Count > 0 ? _responses.Dequeue() : null;
            }

            if (response != null)
                _eventMessageAvailable.Reset();

            return response;
        }

        /// <summary>
        /// Removes all stored responses.
        /// </summary>
        public void ClearResponses()
        {
            lock (_sync)
            {
                _responses.Clear();
            }
        }

        /// <summary>
        /// This method sends a desired request to the GDB.
        /// </summary>
        public bool Send(Request request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            if (IsClosed)
                return false;

            bool canSend = false;

            lock (_sync)
            {
                if (_currentRequest != null && _currentRequest != request)
                {
                    _requests.Enqueue(request);
                }
                else
                {
                    _currentRequest = request;
                    canSend = true;
                }
            }

            if (canSend)
            {
                // serialize the request into string and send it to GDB:
                return request.Execute(_sender);
            }

            return true;
        }

        /// <summary>
        /// Gets next request from the queue and sends it to the GDB.
        /// </summary>
        private bool SendNextRequest()
        {
            Request next = null;

            lock (_sync)
            {
                if (_currentRequest == null && _requests.Count > 0)
                {
                    next = _currentRequest = _requests.Dequeue();
                }
            }

            if (next != null)
            {
                // serialize the request into string and send it to GDB:
                return next.Execute(_sender);
            }

            return true;
        }
    }
}
