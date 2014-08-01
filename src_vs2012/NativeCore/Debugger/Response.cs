using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class representing any response received from GDB.
    /// </summary>
    [DebuggerDisplay("{RawData}")]
    public class Response
    {
        /// <summary>
        /// List of characters that describe a response.
        /// Full info here: http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Output-Syntax.html
        /// </summary>
        private const string ResponseTypeChars = "^*+=~@&";

        private const string ResponseCommentChars = "~@&";

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="rawData">Optional original response received from GDB, before parsing</param>
        /// <param name="id">Identifier of the response (it should match the ID of the request)</param>
        /// <param name="content">Name and content of the response (this value is optional)</param>
        /// <param name="statusOutputs">Additional asynchronous exec and status outputs received along with the result record content</param>
        /// <param name="notifications">Additional asynchronous notifications received along with the result record content</param>
        /// <param name="comments">Additional comments received along with the content</param>
        public Response(string[] rawData, uint id, string content, string[] statusOutputs, string[] notifications, string[] comments)
        {
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentOutOfRangeException("rawData");

            RawData = rawData;
            ID = id;

            if (!string.IsNullOrEmpty(content))
            {
                int argumentsAt = content.IndexOf(',');
                Name = argumentsAt < 0 ? content : content.Substring(0, argumentsAt);
                Content = argumentsAt < 0 ? null : content.Substring(argumentsAt + 1);
            }
            else
            {
                Name = null;
                Content = null;
            }
            StatusOutputs = statusOutputs ?? new string[0];
            Notifications = notifications ?? new string[0];
            Comments = comments ?? new string[0];
        }

        #region Properties

        /// <summary>
        /// The original response received from GDB, before parsing.
        /// </summary>
        public string[] RawData
        {
            get;
            private set;
        }

        public uint ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Name of the result record.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Content arguments passed along with the result record.
        /// </summary>
        public string Content
        {
            get;
            private set;
        }

        public string[] StatusOutputs
        {
            get;
            private set;
        }

        public string[] Notifications
        {
            get;
            private set;
        }

        public string[] Comments
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            return Name;
        }

        public static Response Parse(string[] message)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentNullException("message");

            // split the input into stream-records and result-record:
            string resultRecord = null;
            string resultID = null;
            List<string> streamRecords = null;
            List<string> asyncOutputRecords = null;
            List<string> notificationRecords = null;

            foreach (var line in message)
            {
                if (string.IsNullOrEmpty(line))
                    throw new FormatException("Empty line inside message is not allowed");
                int lineIndex;
                char typeChar = FindTypeChar(line, out lineIndex);

                if (lineIndex < 0)
                {
                    // PH: unknown message type... maybe we could assume it's a comment,
                    // but better double check it...
                    throw new FormatException("Type of the line was not recognized");
                }

                // is comment?
                if (IsCommentLine(typeChar))
                {
                    if (streamRecords == null)
                        streamRecords = new List<string>();
                    streamRecords.Add(line);

                    if (asyncOutputRecords == null)
                        asyncOutputRecords = new List<string>();
                    asyncOutputRecords.Add(line);
                }
                else
                {
                    var type = GetResponseType(typeChar);

                    switch (type)
                    {
                        case ResponseType.ResultRecord:
                            if (resultRecord != null)
                                throw new FormatException("More than one result record is not expected inside GDB message");

                            resultID = lineIndex > 0 ? line.Substring(0, lineIndex) : null;
                            resultRecord = line.Substring(lineIndex + 1);

                            if (asyncOutputRecords == null)
                                asyncOutputRecords = new List<string>();
                            asyncOutputRecords.Add(line.Substring(lineIndex));
                            break;
                        case ResponseType.ExecAsyncOutput: /* fall through */
                        case ResponseType.StatusAsyncOutput:
                            if (asyncOutputRecords == null)
                                asyncOutputRecords = new List<string>();
                            asyncOutputRecords.Add(line);
                            break;
                        case ResponseType.NotificationAsyncOutput:
                            if (notificationRecords == null)
                                notificationRecords = new List<string>();
                            notificationRecords.Add(line);
                            break;
                    }
                }
            }

            // found comments only message:
            if (resultRecord == null)
            {
                return streamRecords == null ? null : new Response(message, 0, null, asyncOutputRecords != null ? asyncOutputRecords.ToArray() : null, notificationRecords != null ? notificationRecords.ToArray() : null, streamRecords.ToArray());
            }

            // get the response identifier
            uint id = 0;
            if (!string.IsNullOrEmpty(resultID))
            {
                // this should be a valid number, otherwise check, why it failed:
                id = uint.Parse(resultID);
            }

            return new Response(message, id, resultRecord, asyncOutputRecords != null ? asyncOutputRecords.ToArray() : null, notificationRecords != null ? notificationRecords.ToArray() : null, streamRecords != null ? streamRecords.ToArray() : null);
        }

        private static ResponseType GetResponseType(char typeChar)
        {
            switch (typeChar)
            {
                case '^':
                    return ResponseType.ResultRecord;
                case '*':
                    return ResponseType.ExecAsyncOutput;
                case '+':
                    return ResponseType.StatusAsyncOutput;
                case '=':
                    return ResponseType.NotificationAsyncOutput;
                case '~': /* fall through */
                case '@': /* fall through */
                case '&':
                    return ResponseType.StreamRecord;
                default:
                    throw new ArgumentOutOfRangeException("typeChar");
            }
        }

        private static bool IsCommentLine(char typeChar)
        {
            return ResponseCommentChars.IndexOf(typeChar) >= 0;
        }

        /// <summary>
        /// Gets the response type character. If the expected char is out of the known list, '\0' and -1 is returned.
        /// It returns also the index, where the char was found, to easier extract the ID, if neeed.
        /// </summary>
        private static char FindTypeChar(string line, out int lineIndex)
        {
            if (string.IsNullOrEmpty(line))
                throw new ArgumentNullException("line");

            int length = line.Length;
            for (int i = 0; i < length; i++)
            {
                if (char.IsDigit(line[i]))
                    continue;

                // is it known response type?
                if (ResponseTypeChars.IndexOf(line[i]) < 0)
                {
                    lineIndex = -1;
                    return '\0';
                }

                lineIndex = i;
                return line[i];
            }

            lineIndex = -1;
            return '\0';
        }
    }
}
