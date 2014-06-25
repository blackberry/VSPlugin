namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class representing single GDB instruction.
    /// </summary>
    public sealed class Instruction
    {
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

        /// <summary>
        /// Gets the response parsing instruction.
        /// </summary>
        public string Response
        {
            get;
            private set;
        }

        #endregion
    }
}
