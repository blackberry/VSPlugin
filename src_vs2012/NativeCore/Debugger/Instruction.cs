using System;
using System.Diagnostics;

namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class representing single GDB instruction.
    /// </summary>
    [DebuggerDisplay("{ID}: {Command}")]
    public sealed class Instruction
    {
        public Instruction(int id, string command, bool expectsParameter, string parsing)
        {
            ID = id;
            Command = command;
            ExpectsParameter = expectsParameter;
            Parsing = parsing;
        }

        #region Properties

        /// <summary>
        /// Gets the ID of the instruction.
        /// </summary>
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the command text expected to be send from GDB.
        /// </summary>
        public string Command
        {
            get;
            private set;
        }

        public bool ExpectsParameter
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the response parsing instruction.
        /// </summary>
        public string Parsing
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Loads data using following format:
        ///     {$}gdb-command:->:parsing-instruction;
        /// It must have the $ sign in front of the GDB command when it is needed to store the command parameters to be used later.
        /// This usually happens whenever the GDB response for a given command does not contains enough information, like 
        /// only "^done" for example, when the parser needs something else.
        /// The ":->:" separates the GDB command from the parsing Instruction.
        /// </summary>
        public static Instruction Load(int id, string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            int separatorIndex = text.IndexOf(":->:", StringComparison.Ordinal);
            if (separatorIndex < 0)
                return null;

            if (text[0] != '$')
                return new Instruction(id, text.Substring(0, separatorIndex), false, text.Substring(separatorIndex + 4));
            return new Instruction(id, text.Substring(1, separatorIndex - 1), true, text.Substring(separatorIndex + 4));
        }
    }
}
