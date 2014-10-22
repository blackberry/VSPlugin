/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace BlackBerry.NativeCore.Components
{
    /// <summary>
    /// Contain a number of functions that handle token replacement
    /// </summary>
    public sealed class TokenProcessor
    {
        #region Token Classes

        private abstract class ActionToken
        {
            public abstract int ID
            {
                get;
            }

            public abstract void UpdateBuffer(StringBuilder buffer);
        }

        /// <summary>
        ///  Storage classes for replacement tokens
        /// </summary>
        [DebuggerDisplay("{ID}: {Token} -> {Replacement}")]
        private class ReplacePairToken : ActionToken
        {
            private readonly int _id;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="id">Identifier of the action</param>
            /// <param name="token">replaceable token</param>
            /// <param name="replacement">replacement string</param>
            public ReplacePairToken(int id, string token, string replacement)
            {
                _id = id;
                Token = token;
                Replacement = replacement;
            }

            /// <summary>
            /// Identifier of the action.
            /// </summary>
            public override int ID
            {
                get { return _id; }
            }

            /// <summary>
            /// Token that needs to be replaced
            /// </summary>
            public string Token
            {
                get;
                private set;
            }

            /// <summary>
            /// String to replace the token with
            /// </summary>
            public string Replacement
            {
                get;
                private set;
            }

            /// <summary>
            /// Replaces the tokens in a buffer with the replacement string
            /// </summary>
            public override void UpdateBuffer(StringBuilder buffer)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                buffer.Replace(Token, Replacement);
            }
        }

        /// <summary>
        /// Storage classes for token to be deleted
        /// </summary>
        [DebuggerDisplay("{ID}: {StringToDelete} -> \"\"")]
        private class DeleteToken : ActionToken
        {
            private readonly int _id;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="id">Identifier of the action</param>
            /// <param name="toDelete">Element to delete</param>
            public DeleteToken(int id, string toDelete)
            {
                _id = id;
                StringToDelete = toDelete;
            }

            /// <summary>
            /// Identifier of the action.
            /// </summary>
            public override int ID
            {
                get { return _id; }
            }

            /// <summary>
            /// Token marking the end of the block to delete
            /// </summary>
            public string StringToDelete
            {
                get;
                private set;
            }

            /// <summary>
            /// Deletes the token from the buffer
            /// </summary>
            public override void UpdateBuffer(StringBuilder buffer)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                buffer.Replace(StringToDelete, string.Empty);
            }
        }

        /// <summary>
        /// Storage classes for string to be deleted between tokens to be deleted 
        /// </summary>
        [DebuggerDisplay("{ID}: {TokenStart}...{TokenEnd} -> {TokenReplacement}")]
        private class ReplaceBetweenPairToken : ActionToken
        {
            private readonly int _id;

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="id">Identifier of the action</param>
            /// <param name="tokenIdentifier">Identifier</param>
            /// <param name="blockStart">Start token</param>
            /// <param name="blockEnd">End Token</param>
            /// <param name="replacement">Replacement string.</param>
            public ReplaceBetweenPairToken(int id, string tokenIdentifier, string blockStart, string blockEnd, string replacement)
            {
                _id = id;
                TokenIdentifier = tokenIdentifier;
                TokenStart = blockStart;
                TokenEnd = blockEnd;
                TokenReplacement = replacement;
            }

            /// <summary>
            /// Identifier of the action.
            /// </summary>
            public override int ID
            {
                get { return _id; }
            }

            /// <summary>
            /// Token marking the beginning of the block to delete
            /// </summary>
            public string TokenStart
            {
                get;
                private set;
            }

            /// <summary>
            /// Token marking the end of the block to delete
            /// </summary>
            public string TokenEnd
            {
                get;
                private set;
            }

            /// <summary>
            /// Token marking the end of the block to delete
            /// </summary>
            public string TokenReplacement
            {
                get;
                private set;
            }

            /// <summary>
            /// Token Identifier
            /// </summary>
            public string TokenIdentifier
            {
                get;
                private set;
            }

            /// <summary>
            /// Replaces the token from the buffer between the provided tokens
            /// </summary>
            public override void UpdateBuffer(StringBuilder buffer)
            {
                if (buffer == null)
                    throw new ArgumentNullException("buffer");

                // PH: do we really need regexp here?... don't care for now, as this code looks like not used anywhere...
                string regularExp = TokenStart + "[^" + TokenIdentifier + "]*" + TokenEnd;
                string text = buffer.ToString();
                buffer.Clear();
                buffer.Append(Regex.Replace(text, regularExp, TokenReplacement));
            }
        }

        #endregion

        private readonly List<ActionToken> _tokens;
        private int _globalID;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TokenProcessor()
        {
            _tokens = new List<ActionToken>();
        }

        private int GetNextID()
        {
            return Interlocked.Increment(ref _globalID);
        }

        /// <summary>
        /// Reset the list of replacer entries.
        /// </summary>
        public void Reset()
        {
            _tokens.Clear();
        }

        /// <summary>
        /// Remove action with specified id.
        /// </summary>
        /// <param name="id">Unique identifier of an action to remove</param>
        public bool Remove(int id)
        {
            for(int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].ID == id)
                {
                    _tokens.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Remove all actions with specified identifiers.
        /// </summary>
        /// <param name="identifiers">Collection of unique identifiers of actions to remove</param>
        public void Remove(IEnumerable<int> identifiers)
        {
            if (identifiers == null)
                throw new ArgumentNullException("identifiers");

            foreach(var id in identifiers)
            {
                Remove(id);
            }
        }

        /// <summary>
        /// Add a replacement type entry.
        /// </summary>
        /// <param name="token">Token to replace</param>
        /// <param name="replacement">Replacement string</param>
        public int AddReplace(string token, string replacement)
        {
            var id = GetNextID();
            _tokens.Add(new ReplacePairToken(id, token, replacement));
            return id;
        }

        /// <summary>
        /// Add replacement between entry.
        /// </summary>
        /// <param name="tokenIdentifier">Identifier</param>
        /// <param name="tokenStart">Start token</param>
        /// <param name="tokenEnd">End token</param>
        /// <param name="replacement">Replacement for found entry</param>
        public int AddReplaceBetween(string tokenIdentifier, string tokenStart, string tokenEnd, string replacement)
        {
            var id = GetNextID();
            _tokens.Add(new ReplaceBetweenPairToken(id, tokenIdentifier, tokenStart, tokenEnd, replacement));
            return id;
        }

        /// <summary>
        /// Add a deletion entry.
        /// </summary>
        /// <param name="tokenToDelete">Token to delete</param>
        public int AddDelete(string tokenToDelete)
        {
            var id = GetNextID();
            _tokens.Add(new DeleteToken(id, tokenToDelete));
            return id;
        }

        #region TokenProcessing

        /// <summary>
        /// For all known tokens, replace them with correct values inside given string.
        /// </summary>
        public string Untoken(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var buffer = new StringBuilder(text);
            Untoken(buffer);
            return buffer.ToString();
        }

        /// <summary>
        /// For all known tokens, replace them with correct values
        /// </summary>
        /// <param name="source">Full path of the source file</param>
        /// <param name="destination">Full path of the destination file</param>
        public string UntokenFile(string source, string destination)
        {
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException("source");
            if (string.IsNullOrEmpty(destination))
                throw new ArgumentNullException("destination");

            // make sure that the destination folder exists:
            string destinationFolder = Path.GetDirectoryName(destination);
            if (string.IsNullOrEmpty(destinationFolder))
                throw new ArgumentOutOfRangeException("destination");

            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            // Open the file. Check to see if the File is binary or text.
            // NOTE: This is not correct because GetBinaryType will return true
            // only if the file is executable, not if it is a dll, a library or
            // any other type of binary file.

            // PH: FIXME: No idea, why someone checks for exe/dll binary kind here (x86/x64/DOS/...)...
            uint binaryType;
            if (!NativeMethods.GetBinaryType(source, out binaryType))
            {
                Encoding encoding;
                StringBuilder buffer;

                // Create the reader to get the text. Note that we will default to ASCII as
                // encoding if the file does not contains a different signature.
                using (StreamReader reader = new StreamReader(source, Encoding.ASCII, true))
                {
                    // Get the content of the file.
                    buffer = new StringBuilder(reader.ReadToEnd());

                    // Detect the encoding of the source file. Note that we
                    // can get the encoding only after a read operation is
                    // performed on the file.
                    encoding = reader.CurrentEncoding;
                }

                Untoken(buffer);
                File.WriteAllText(destination, buffer.ToString(), encoding);
            }
            else
                File.Copy(source, destination);

            return destination;
        }

        private void Untoken(StringBuilder buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            foreach (var token in _tokens)
            {
                token.UpdateBuffer(buffer);
            }
        }

        #endregion

        #region Guid generators

        /// <summary>
        /// Generates a string representation of a guid with the following format:
        /// 0x01020304, 0x0506, 0x0708, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10
        /// </summary>
        /// <param name="value">Guid to be generated</param>
        /// <returns>The guid as string</returns>
        public static string GuidToForm(Guid value)
        {
            var guidBytes = value.ToByteArray();
            var result = new StringBuilder(80);

            // first 4 bytes
            uint number = 0;
            int i;
            for (i = 0; i < 4; ++i)
            {
                number <<= 8;
                number += guidBytes[i];
            }
            result.Append("0x").Append(number.ToString("X8", CultureInfo.InvariantCulture));

            // 2 chunks of 2 bytes
            for (int j = 0; j < 2; ++j)
            {
                number = guidBytes[i++];
                number <<= 8;
                number += guidBytes[i++];
                result.Append(", 0x").Append(number.ToString("X4", CultureInfo.InvariantCulture));
            }

            // 8 chunks of 1 bytes
            for (int j = 0; j < 8; ++j)
            {
                result.Append(", 0x").Append(guidBytes[i++].ToString("X2", CultureInfo.InvariantCulture));
            }

            return result.ToString();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// This function will accept a subset of the characters that can create an
        /// identifier name: there are other unicode char that can be inside the name, but
        /// this function will not allow.
        /// </summary>
        /// <param name="c">Character to validate</param>
        /// <returns>true if successful false otherwise</returns>
        public static bool IsValidIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        /// <summary>
        /// Verifies if the start character is valid
        /// </summary>
        /// <param name="c">Start character</param>
        /// <returns>true if successful false otherwise</returns>
        public static bool IsValidIdentifierStartChar(char c)
        {
            return IsValidIdentifierChar(c) && !char.IsDigit(c);
        }

        #endregion
    }
}
