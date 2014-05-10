using System;
using System.Diagnostics;
using System.Text;

namespace RIM.VSNDK_Package.Diagnostics
{
    /// <summary>
    /// Helper class for printing trace log messages.
    /// </summary>
    internal static class TraceLog
    {
        public const string Category = "BlackBerry";

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
        public static void WriteException(Exception ex)
        {
            if (ex == null)
                return;

            Trace.WriteLine(FlatException(ex), Category);
        }

        /// <summary>
        /// Converts exception data into a single string.
        /// </summary>
        private static string FlatException(Exception ex)
        {
            var result = new StringBuilder();

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
