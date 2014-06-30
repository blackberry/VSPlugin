using System;
using System.Diagnostics;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB and helps in passing commands in both directions.
    /// </summary>
    public class GdbRunner : ToolRunner, IGdbSender
    {
        private GdbProcessor _processor;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="gdb">Description of the GDB to communicate with.</param>
        public GdbRunner(GdbInfo gdb)
            : base(gdb != null ? gdb.Executable : null, null)
        {
            if (gdb == null)
                throw new ArgumentNullException("gdb");

            GDB = gdb;
            _processor = new GdbProcessor(this);
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="fileName">Path to the executable, invoked, when starting this tool.</param>
        /// <param name="gdb">Description of the GDB to communicate with.</param>
        protected GdbRunner(string fileName, GdbInfo gdb)
            : base(fileName, null)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");
            if (gdb == null)
                throw new ArgumentNullException("gdb");

            GDB = gdb;
            _processor = new GdbProcessor(this);
        }

        #region Properties

        /// <summary>
        /// Gets the startup info about the GDB itself.
        /// </summary>
        public GdbInfo GDB
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the runtime state and events from the GDB.
        /// </summary>
        public GdbProcessor Processor
        {
            get { return _processor; }
        }

        /// <summary>
        /// Gets or sets showing of the GDB console.
        /// Please note, that when console is shown, output is not consumed as it is not delivered to Processor.
        /// </summary>
        public bool ShowConsole
        {
            get { return ShowWindow; }
            set { ShowWindow = value; }
        }

        #endregion

        #region IGdbSender Implementation

        void IGdbSender.Break()
        {
            Break();
        }

        bool IGdbSender.Send(string text)
        {
#if DEBUG
            Debug.WriteLine(string.Format("GDB-INPUT : {0}", text));
#endif

            return SendInput(text);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_processor != null)
                {
                    _processor.Dispose();
                    _processor = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Command Processing

        /// <summary>
        /// Sends Ctrl+C to the GDB process.
        /// </summary>
        public virtual void Break()
        {
            throw new NotSupportedException("Breaking is not supported by GDB in this type of call");
        }

        protected override void ProcessOutputLine(string text)
        {
#if DEBUG
            Debug.WriteLine(string.Format("GDB-OUTPUT: {0}", text));
#endif

            if (_processor != null)
            {
                _processor.Receive(text);
            }
        }

        #endregion
    }
}
