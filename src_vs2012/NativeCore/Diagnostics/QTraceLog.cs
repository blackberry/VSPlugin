using System;
using System.Diagnostics;
using System.Text;

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

        /// <summary>
        /// Dumps an array into the logs.
        /// </summary>
        [Conditional("TRACE")]
        [DebuggerStepThrough]
        public static void PrintArray(string name, byte[] values)
        {
            StringBuilder result = new StringBuilder();

            // print byte array for Java:
            result.Append("byte[] ").Append(name).Append(" = new byte[ /* ").Append(values.Length).Append(" */ ] {");
            int i = 0;
            foreach (byte v in values)
            {
                if (i > 0)
                    result.Append(", ");
                i++;

                // print signed value:
                result.Append((sbyte)v);
            }
            result.Append("};");

            // and the same array for C#:
            result.AppendLine();
            result.Append("// byte[] ").Append(name).Append("CS = new byte[ /* ").Append(values.Length).Append(" */ ] {");
            i = 0;
            foreach (byte v in values)
            {
                if (i > 0)
                    result.Append(", ");
                i++;

                // print signed value:
                result.Append(v);
            }
            result.Append("};");

            // and also print it out as string:
            result.AppendLine();
            result.Append("String ").Append(name).Append("Str = \"00"); // fince in Java BigInteger is 2-complement, so we start with leading zeros, not to have a minus numbers...
            foreach (var v in values)
            {
                result.Append(v.ToString("X2"));
            }
            result.Append("\";");

            Trace.WriteLine(result.ToString());
        }
    }
}
