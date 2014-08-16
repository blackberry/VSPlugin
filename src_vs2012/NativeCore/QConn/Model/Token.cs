using System;
using System.Collections.Generic;
using System.Globalization;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Part of the response received from a TargetFileService.
    /// </summary>
    public sealed class Token
    {
        private readonly string _token;

        /// <summary>
        /// Init constructor.
        /// </summary>
        /// <param name="token"></param>
        public Token(string token)
        {
            if (token != null && token.Length >= 2 && token[0] == '"' && token[token.Length - 1] == '"')
                _token = token.Substring(1, token.Length - 2);
            else
                _token = token ?? string.Empty;
        }

        #region Properties

        /// <summary>
        /// Gets the UInt32 value of the token.
        /// </summary>
        public uint UInt32Value
        {
            get { return uint.Parse(_token, NumberStyles.HexNumber); }
        }

        /// <summary>
        /// Gets the UInt64 value of the token.
        /// </summary>
        public ulong UInt64Value
        {
            get { return ulong.Parse(_token, NumberStyles.HexNumber); }
        }

        /// <summary>
        /// Gets the string value of the token (wrapping '"' will be removed).
        /// </summary>
        public string StringValue
        {
            get { return _token; }
        }

        #endregion

        public override string ToString()
        {
            return _token;
        }

        /// <summary>
        /// Returns an array of tokens parsed from specified string. Each token ends with ':' delimiter.
        /// </summary>
        public static Token[] Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            var result = new List<Token>();
            bool isInString = false;
            int startAt = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '"')
                {
                    isInString = !isInString;
                }
                else
                    if (!isInString && text[i] == ':')
                    {
                        // ok, found end of token:
                        result.Add(new Token(text.Substring(startAt, i - startAt)));
                        startAt = i + 1;
                    }
            }
            
            // and add the closing token, that has no delimiter:
            result.Add(new Token(text.Substring(startAt, text.Length - startAt)));
            return result.ToArray();
        }
    }
}
