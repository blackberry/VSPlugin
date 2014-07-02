namespace BlackBerry.NativeCore.Debugger
{
    /// <summary>
    /// Class wrapping the regular command send to the GDB.
    /// </summary>
    public sealed class CliRequest : Request
    {
        public CliRequest(string command)
            : base(command)
        {
        }

        public override string ToString()
        {
            // CLI commands have no '-' in front of them
            return string.Concat(ID, Command);
        }
    }
}
