using System;
using System.Collections.Generic;
using System.Text;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GdbProcessor : IDisposable
    {
        private const string Prompt = "(gdb) ";

        private readonly IGdbSender _sender;
        private readonly StringBuilder _messageCache;
        private readonly Queue<string> _messages;
        private readonly object _sync;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public GdbProcessor(IGdbSender sender)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");

            _sender = sender;
            _messageCache = new StringBuilder();
            _messages = new Queue<string>();
            _sync = new object();
        }

        ~GdbProcessor()
        {
            Dispose(false);
        }

        #region IDisposable Implemenation

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool dispose)
        {
            
        }

        #endregion

        /// <summary>
        /// Stores text received from GDB for further processing.
        /// </summary>
        /// <returns>Returns 'true', when whole new message has been read and there is something to process.</returns>
        public bool Receive(string text)
        {
            lock (_sync)
            {
                // is it an end of the message?
                if (text == Prompt)
                {
                    var message = _messageCache.ToString();
                    _messageCache.Remove(0, _messageCache.Length);

                    // then store it separately for further processing:
                    if (!string.IsNullOrEmpty(message))
                    {
                        _messages.Enqueue(message);
                        return true;
                    }
                }
                else
                {
                    _messageCache.AppendLine(text);
                }

                return false;
            }
        }
    }
}
