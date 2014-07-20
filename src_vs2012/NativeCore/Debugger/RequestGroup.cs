using System;
using System.Collections.Generic;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Represents a group of requests that should be threated as single request.
    /// </summary>
    public sealed class RequestGroup : Request
    {
        private readonly List<Request> _requests;
        private int _activeIndex;

        public RequestGroup()
        {
            _requests = new List<Request>();
            _activeIndex = 0;
        }

        #region Properties

        public Request ActiveRequest
        {
            get { return _activeIndex >= 0 && _activeIndex < _requests.Count ? _requests[_activeIndex] : null; }
        }

        public override uint ID
        {
            get
            {
                var request = ActiveRequest;
                if (request == null)
                    throw new InvalidOperationException("Current group has no active request");
                return request.ID;
            }
        }

        public override string Command
        {
            get
            {
                var request = ActiveRequest;
                if (request == null)
                    throw new InvalidOperationException("Current group has no active request");

                return request.Command;
            }
        }

        public override Response Response
        {
            get
            {
                var request = ActiveRequest;
                if (request == null)
                    throw new InvalidOperationException("Current group has no active request");

                return request.Response;
            }
        }

        #endregion

        public override string ToString()
        {
            var request = ActiveRequest;
            return request == null ? string.Empty : ActiveRequest.ToString();
        }

        public void Add(Request request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            _requests.Add(request);
        }

        public override bool Complete(Response response, out bool retry)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            var request = ActiveRequest;
            if (request == null)
                throw new InvalidOperationException("Current group has no active request");

            bool result = ActiveRequest.Complete(response, out retry);
            if (result)
            {
                // is it the last request?
                if (_activeIndex == _requests.Count - 1)
                {
                    retry = false;
                    SetEvent();
                    return true;
                }

                // send next request from this group:
                retry = true;
                _activeIndex++;
            }

            return false;
        }
    }
}
