using System;
using System.Collections.Generic;
using System.Diagnostics;
using BlackBerry.NativeCore.Debugger.Model;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Log entry description.
    /// </summary>
    [DebuggerDisplay("{Message}")]
    public sealed class TargetLogEntry
    {
        public enum LogType
        {
            Console,
            SLog2
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        private TargetLogEntry(LogType type, uint pid, string message)
        {
            Type = type;
            PID = pid;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        private TargetLogEntry(LogType type, uint pid, string appID, string bufferSet, string message)
        {
            Type = type;
            PID = pid;
            AppID = appID;
            BufferSet = bufferSet;
            Message = message ?? string.Empty;
        }

        #region Properties

        /// <summary>
        /// Gets the process ID issuing the log entry.
        /// </summary>
        public uint PID
        {
            get;
            private set;
        }

        public LogType Type
        {
            get;
            private set;
        }

        public string AppID
        {
            get;
            private set;
        }

        public string BufferSet
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Converts the raw console outputs into featured log-class collection.
        /// </summary>
        public static TargetLogEntry[] ParseConsole(ProcessInfo process, string[] nativeLogEntries)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            if (nativeLogEntries != null && nativeLogEntries.Length > 0)
            {
                var result = new TargetLogEntry[nativeLogEntries.Length];

                for (int i = 0; i < nativeLogEntries.Length; i++)
                {
                    result[i] = new TargetLogEntry(LogType.Console, process.ID, nativeLogEntries[i]);
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Converts raw slog2 outputs into featured log-class collection.
        /// </summary>
        public static TargetLogEntry[] ParseSLog2(string[] nativeLogEntries)
        {
            if (nativeLogEntries != null && nativeLogEntries.Length > 0)
            {
                var result = new List<TargetLogEntry>();
                TargetLogEntry lastEntry = null;

                foreach (var nativeEntry in nativeLogEntries)
                {
                    // ignore empty lines:
                    if (string.IsNullOrEmpty(nativeEntry))
                        continue;

                    var parsedEntry = ParseSLog2Entry(nativeEntry);

                    if (parsedEntry != null)
                    {
                        lastEntry = parsedEntry;
                        result.Add(parsedEntry);
                    }
                    else
                    {
                        // is it a part of multiline message (like dump of PPS parameters, where each line is not prefixed with slog header):
                        if (lastEntry != null)
                        {
                            lastEntry.Message += Environment.NewLine + nativeEntry;
                        }
                        else
                        {
                            // or it's just orphan text (let the upper layer care about it):
                            result.Add(new TargetLogEntry(LogType.SLog2, 0, nativeEntry));
                        }
                    }
                }

                return result.Count > 0 ? result.ToArray() : null;
            }

            return null;
        }

        /// <summary>
        /// Parses single line of slog2, which typically looks like following:
        /// 
        /// Dec 04 03:05:54.970 com.codetitans.FallingBlocks.testDev_llingBlocksb07fdb40.383152370              default   8900  5  PPS helper destructor
        /// 
        /// </summary>
        private static TargetLogEntry ParseSLog2Entry(string nativeEntry)
        {
            int i;

            // skip month:
            i = nativeEntry.IndexOf(' ', 0);
            if (i < 0)
                return null;

            // skip day:
            i = nativeEntry.IndexOf(' ', i + 1);
            if (i < 0)
                return null;

            // skip time:
            i = nativeEntry.IndexOf(' ', i + 1);
            if (i < 0)
                return null;

            // skip all white-chars:
            if (SkipSpaces(nativeEntry, ref i))
                return null;

            // read the application-id + process-id:
            int idEndAt = nativeEntry.IndexOf(' ', i + 1);
            if (idEndAt < 0)
                return null;

            string sandboxInfo = nativeEntry.Substring(i, idEndAt - i);
            if (string.IsNullOrEmpty(sandboxInfo))
                return null;

            // jump to the beginning of the buffer name:
            i = idEndAt;
            if (SkipSpaces(nativeEntry, ref i))
                return null;

            int bufferSetStartAt = i;
            int bufferSetEndAt = nativeEntry.IndexOf(' ', i + 1);
            if (bufferSetEndAt < 0)
                return null;

            i = bufferSetEndAt;
            if (nativeEntry[bufferSetEndAt - 1] == '*')
                bufferSetEndAt--;

            string bufferSetID = nativeEntry.Substring(bufferSetStartAt, bufferSetEndAt - bufferSetStartAt);

            // PH: this means, there is no bufferSet and we already read the first number
            if (bufferSetID == "0")
            {
                bufferSetID = "default";
            }
            else
            {
                // skip spaces till the first number:
                if (SkipSpaces(nativeEntry, ref i))
                    return null;

                // skip the first number:
                i = nativeEntry.IndexOf(' ', i + 1);
                if (i < 0)
                    return null;
            }

            // move to the second number:
            if (SkipSpaces(nativeEntry, ref i))
                return null;

            // skip the second number:
            i = nativeEntry.IndexOf(' ', i + 1);
            if (i < 0)
                return null;

            // message is prefixed with two spaces:
            if (i < nativeEntry.Length - 2)
                i += 2;

            string message = nativeEntry.Substring(i);
            string appID;
            uint processID = SplitToProcessAndAppID(sandboxInfo, out appID);
            return new TargetLogEntry(LogType.SLog2, processID, appID, bufferSetID, message);
        }

        private static bool SkipSpaces(string text, ref int i)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
            if (i == text.Length)
                return true;
            return false;
        }

        private static uint SplitToProcessAndAppID(string text, out string appID)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            // card apps can spawn multiple instances of the same application
            // then they share the same appID-base part, but end with "..<number>"
            int i = text.Length - 1;
            int instanceSuffix = char.IsDigit(text[i]) && text[i - 1] == '.' && text[i - 2] == '.' ? 3 : 0;
            int processIdStartAt = text.LastIndexOf('.', i - instanceSuffix);

            if (processIdStartAt < 0)
            {
                appID = text;
                return 0;
            }

            string processIdText = text.Substring(processIdStartAt + 1, i - instanceSuffix - processIdStartAt);
            uint id;

            if (uint.TryParse(processIdText, out id))
            {
                appID = text.Substring(0, processIdStartAt);
                return id;
            }

            appID = text;
            return 0;
        }
    }
}
