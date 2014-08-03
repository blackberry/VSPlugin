namespace BlackBerry.NativeCore.Debugger.Requests
{
    /// <summary>
    /// Class wrapping the regular CLI-command send to the GDB.
    /// </summary>
    public class CliRequest : Request
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
