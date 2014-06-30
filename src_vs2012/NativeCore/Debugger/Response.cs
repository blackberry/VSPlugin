using System;

namespace BlackBerry.NativeCore.Debugger
{
    public class Response
    {
        /// <summary>
        /// List of characters that describe a response.
        /// Full info here: http://sourceware.org/gdb/onlinedocs/gdb/GDB_002fMI-Output-Syntax.html
        /// </summary>
        private static readonly char[] ResponseStartChars = new[] { '^', '*', '+', '=', '~', '@', '&' };

        public Response(string id, ResponseType type, string content)
        {
            if (string.IsNullOrEmpty(content))
                throw new ArgumentNullException("content");

            ID = id;
            Type = type;
            Content = content;
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

        public string Content
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            return Content;
        }

        public static Response Parse(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message");

            // locate the beginning of the last line:
            int startAt = message.LastIndexOf(Environment.NewLine, message.EndsWith(Environment.NewLine) ? message.Length - Environment.NewLine.Length : message.Length, StringComparison.Ordinal);
            if (startAt < 0)
                startAt = 0;
            else
                startAt += Environment.NewLine.Length;

            int tokenEnd = message.IndexOfAny(ResponseStartChars, startAt);
            if (tokenEnd < 0)
            {
                if (startAt > 0)
                {
                    startAt = 0;
                    tokenEnd = message.IndexOfAny(ResponseStartChars, startAt);
                }

                if (tokenEnd < 0)
                    throw new FormatException("Invalid message received from GDB");
            }

            string id = tokenEnd == 0 ? null : message.Substring(startAt, tokenEnd - startAt);
            char typeChar = message[tokenEnd];

            switch (typeChar)
            {
                case '^':
                    return ParseResultRecord(id, message.Substring(tokenEnd + 1));
            }
            
            return new Response(id, ResponseType.ResultRecord, message);
        }

        private static Response ParseResultRecord(string id, string message)
        {
            return new Response(id, ResponseType.ResultRecord, message);
        }
    }
}
