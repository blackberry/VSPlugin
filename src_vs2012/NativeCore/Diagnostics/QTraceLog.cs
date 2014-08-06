using System;
using System.Diagnostics;

namespace BlackBerry.NativeCore.Diagnostics
{
    /// <summary>
    /// Helper class for printing QNX trace log messages.
    /// </summary>
    public static class QTraceLog
    {
        public const string Category = "QConn";

        /// <summary>
        /// Writes a debug message.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WriteLine(string message)
        {
            if (message == null)
                return;

            Trace.WriteLine(message, Category);
        }

        /// <summary>
        /// Writes a debug message.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WriteLine(string format, params object[] args)
        {
            if (format == null)
                return;

            Trace.WriteLine(string.Format(format, args), Category);
        }

        /// <summary>
        /// Writes info about the exception.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WriteException(Exception ex)
        {
            if (ex == null)
                return;

            Trace.WriteLine(TraceLog.FlatException(ex, null), Category);
        }

        /// <summary>
        /// Writes info about the exception.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WriteException(Exception ex, string message)
        {
            if (ex == null)
            {
                WriteLine(message);
            }
            else
            {
                Trace.WriteLine(TraceLog.FlatException(ex, message), Category);
            }
        }

        /// <summary>
        /// Writes info about the exception.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WriteException(Exception ex, string format, params object[] args)
        {
            if (ex == null)
            {
                WriteLine(format, args);
            }
            else
            {
                string message = format == null ? null : string.Format(format, args);
                Trace.WriteLine(TraceLog.FlatException(ex, message), Category);
            }
        }
    }
}
