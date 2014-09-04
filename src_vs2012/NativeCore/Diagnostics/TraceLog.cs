using System;
using System.Diagnostics;
using System.Text;

namespace BlackBerry.NativeCore.Diagnostics
{
    /// <summary>
    /// Helper class for printing trace log messages.
    /// </summary>
    public static class TraceLog
    {
        public const string Category = "BlackBerry";
        public const string CategoryGDB = "GDB";

        /// <summary>
        /// Add new trace-log message listener.
        /// </summary>
        [Conditional("TRACE")]
        public static void Add(TraceListener listener)
        {
            if (listener != null)
            {
                Trace.Listeners.Add(listener);
            }
        }

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
        /// Writes a warning message.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WarnLine(string message)
        {
            if (message == null)
                return;

            Trace.WriteLine("!! " + message, Category);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void WarnLine(string format, params object[] args)
        {
            if (format == null)
                return;

            Trace.WriteLine("!! " + string.Format(format, args), Category);
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

            Trace.WriteLine(FlatException(ex, null), Category);
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
                Trace.WriteLine(FlatException(ex, message), Category);
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
                Trace.WriteLine(FlatException(ex, message), Category);
            }
        }

        /// <summary>
        /// Converts exception data into a single string.
        /// </summary>
        [DebuggerStepThrough]
        internal static string FlatException(Exception ex, string message)
        {
            var result = new StringBuilder();

            if (message != null)
                result.AppendLine(message);

            while (ex != null)
            {
                // get info:
                result.Append("### (").Append(ex.GetType().Name).Append(") ").AppendLine(ex.Message);
                result.AppendLine(ex.StackTrace);

                // switch to inner one:
                ex = ex.InnerException;
            }

            return result.ToString();
        }
    }
}
