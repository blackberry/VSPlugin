using System;
using BlackBerry.NativeCore.Debugger;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB and helps in passing commands in both directions.
    /// </summary>
    public sealed class GdbRunner : ToolRunner, IGdbSender
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

        #region Properties

        public GdbInfo GDB
        {
            get;
            private set;
        }

        public bool ShowConsole
        {
            get { return ShowWindow; }
            set { ShowWindow = value; }
        }

        #endregion

        #region IGdbSender Implementation

        void IGdbSender.Break()
        {
            throw new NotSupportedException("Breaking is not supported by GDB in this type of call");
        }

        void IGdbSender.Send(string text)
        {
            SendInput(text);
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
    }
}
