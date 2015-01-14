using System;
using System.Collections.Generic;
using System.Text;
using BlackBerry.NativeCore.Helpers;
using BlackBerry.NativeCore.QConn.Services;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing extra functionalities around running slog2info on a target device.
    /// </summary>
    public sealed class TargetProcessSLog2Info : TargetProcess
    {
        private byte[] _lastLine;

        public event EventHandler<CapturedLogsEventArgs> Captured;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetProcessSLog2Info(TargetServiceLauncher service, QConnConnection connection, uint pid, bool suspended)
            : base(service, connection, pid, suspended)
        {
        }

        #region IDisposable Implementation

        /// <summary>
        /// Disposing used resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            Captured = null;
            base.Dispose(disposing);
        }

        #endregion

        #region Processing Console Output

        protected override void ProcessOutputInitialize()
        {
            _lastLine = null;
        }

        protected override bool ProcessOutputData(HResult result, byte[] data)
        {
            if (Captured != null)
            {
                // new data arrived, split it into lines of text:
                var logEntries = ReadLines(data, ref _lastLine);

                // then parse and forward them:
                var targetLogs = TargetLogEntry.ParseSLog2(logEntries);

                if (targetLogs != null)
                {
                    Captured(this, new CapturedLogsEventArgs(targetLogs));
                }
            }

            return false;
        }

        private string[] ReadLines(byte[] data, ref byte[] lastLine)
        {
            if (data == null || data.Length == 0)
                return null;

            var result = new List<string>();
            int lineStart = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 10) // '\n'
                {
                    // is it the first line?
                    if (lineStart < 0)
                    {
                        AddString(result, GetString(lastLine, data, lineStart + 1, i));
                        lastLine = null;
                    }
                    else
                    {
                        AddString(result, GetString(null, data, lineStart + 1, i - lineStart - 1));
                    }

                    // move the end of line marker:
                    lineStart = i + 1 < data.Length && data[i + 1] == 13 ? i + 1 : i;
                }
            }

            // the end of the message is not a properly finished new line:
            if (lineStart < 0 || lineStart != data.Length - 1)
            {
                lastLine = BitHelper.Combine(lastLine, data, lineStart + 1, data.Length - lineStart - 1);
            }

            return result.Count > 0 ? result.ToArray() : null;
        }

        private void AddString(List<string> result, string message)
        {
            if (message == null)
                return;

            if (result.Count == 0 || string.Compare(result[result.Count - 1], message, StringComparison.Ordinal) != 0)
            {
                result.Add(message);
            }
        }

        private string GetString(byte[] lineStart, byte[] data, int dataFrom, int dataLength)
        {
            return Encoding.UTF8.GetString(BitHelper.Combine(lineStart, data, dataFrom, dataLength));
        }

        #endregion
    }
}
