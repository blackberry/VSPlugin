namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Arguments passed along with response received from GDB and processed by parsing instructions of GdbWrapper.
    /// </summary>
    public sealed class ResponseParsedEventArgs : ResponseReceivedEventArgs
    {
        /// <summary>
        /// Init constructor.
        /// </summary>
        public ResponseParsedEventArgs(Request request, Response response, bool handled, string parsedResult)
            : base(request, response, handled)
        {
            ParsedResult = parsedResult;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public ResponseParsedEventArgs(ResponseReceivedEventArgs e, string parsedResult)
            : base(e != null ? e.Request : null, e != null ? e.Response : null, e != null && e.Handled)
        {
            ParsedResult = parsedResult;
        }

        #region Properties

        public string ParsedResult
        {
            get;
            private set;
        }

        #endregion
    }
}
