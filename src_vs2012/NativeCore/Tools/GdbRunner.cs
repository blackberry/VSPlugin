using System;
using BlackBerry.NativeCore.Model;

namespace BlackBerry.NativeCore.Tools
{
    /// <summary>
    /// Runner, that starts up the GDB and helps in passing commands in both directions.
    /// </summary>
    public sealed class GdbRunner : ToolRunner
    {
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
    }
}
