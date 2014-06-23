using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BlackBerry.NativeCore.Diagnostics
{
    /// <summary>
    /// Trace listener, that writes BlackBerry-only messages into the file.
    /// </summary>
    public sealed class BlackBerryTraceListener : TraceListener
    {
        public const string SName = "Persistent BlackBerry Trace Listener";

        private TextWriter _output;
        private readonly TimeTracker _time;

        /// <summary>
        /// Inti constructor.
        /// </summary>
        public BlackBerryTraceListener(string fileName, bool printTime)
            : this(string.IsNullOrEmpty(fileName) ? null : File.CreateText(fileName), printTime)
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BlackBerryTraceListener(Stream stream, Encoding encoding, bool printTime)
            : this((stream == null ? null : new StreamWriter(stream, encoding)), printTime)
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public BlackBerryTraceListener(TextWriter output, bool printTime)
            : base(SName)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            _output = output;
            if (printTime)
                _time = new TimeTracker();
        }

        protected override void Dispose(bool disposing)
        {
            if (_output != null)
            {
                ((IDisposable)_output).Dispose();
                _output = null;
            }
        }

        public override void Flush()
        {
            if (_output != null)
            {
                _output.Flush();
            }
        }

        public override void Write(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void WriteLine(string message)
        {
            // do nothing, only want to capture filtered-by-category messages
        }

        public override void Write(string message, string category)
        {
            if (_output == null)
                return;

            // print only messages of 'BlackBerry' category:
            if (string.CompareOrdinal(category, TraceLog.Category) != 0)
                return;

            if (_time != null)
                _time.Write(_output);
            _output.Write(message);
        }

        public override void WriteLine(string message, string category)
        {
            if (_output == null)
                return;

            // print only messages of 'BlackBerry' category:
            if (string.CompareOrdinal(category, TraceLog.Category) != 0)
                return;

            if (_time != null)
                _time.WriteAndReset(_output);
            _output.WriteLine(message);
        }
    }
}
