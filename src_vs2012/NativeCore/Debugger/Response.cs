using System;
using System.Collections.Generic;

namespace BlackBerry.NativeCore.Debugger
{
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
        /// <param name="id">Identifier of the response (it should match the ID of the request)</param>
        /// <param name="type">Type of the response</param>
        /// <param name="content">Name and content of the response. This value can not be empty.</param>
        /// <param name="notifications">Additional asynchronous notifications received along with the content</param>
        /// <param name="comments">Additional comments received along with the content</param>
        public Response(string id, ResponseType type, string content, string[] notifications, string[] comments)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException("content");

            ID = id;
            Type = type;

            if (Type != ResponseType.StreamRecord)
            {
                int argumentsAt = content.IndexOf(',');
                Name = argumentsAt < 0 ? content : content.Substring(0, argumentsAt);
                Content = argumentsAt < 0 ? null : content.Substring(argumentsAt + 1);
            }
            else
            {
                Name = null;
                Content = content;
            }
            Notifications = notifications ?? new string[0];
            Comments = comments ?? new string[0];
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="notifications">Additional asynchronous notifications received along with the content</param>
        /// <param name="comments">Set of comments. This value can not be empty</param>
        public Response(string[] notifications, string[] comments)
        {
            if (comments == null || comments.Length == 0)
                throw new ArgumentOutOfRangeException("comments");

            ID = null;
            Type = ResponseType.StreamRecord;
            Name = null;
            Content = null;
            Notifications = notifications ?? new string[0];
            Comments = comments;
        }

        #region Properties

        public string ID
        {
            get;
            private set;
        }

        public ResponseType Type
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Content
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
            ResponseType resultType = ResponseType.ResultRecord;
            List<string> streamRecords = null;
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
                }
                else
                {
                    resultType = GetResponseType(typeChar);

                    if (resultType == ResponseType.ResultRecord)
                    {
                        if (resultRecord != null)
                            throw new FormatException("More than one result record is not expected inside GDB message");

                        resultID = lineIndex > 0 ? line.Substring(0, lineIndex) : null;
                        resultRecord = line.Substring(lineIndex + 1);
                    }
                    else
                    {
                        if (notificationRecords == null)
                            notificationRecords = new List<string>();
                        notificationRecords.Add(line);
                    }
                }
            }

            // found comments only message:
            if (resultRecord == null)
            {
                return streamRecords == null ? null : new Response(notificationRecords.ToArray(), streamRecords.ToArray());
            }

            return new Response(resultID, resultType, resultRecord, notificationRecords != null ? notificationRecords.ToArray() : null, streamRecords != null ? streamRecords.ToArray() : null);
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
