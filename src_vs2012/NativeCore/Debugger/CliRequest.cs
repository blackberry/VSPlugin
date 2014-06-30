namespace BlackBerry.NativeCore.Debugger
{
    public class CliRequest : Request
    {
        public CliRequest(string command)
            : base(command)
        {
        }

        public override string ToString()
        {
            // CLI commands have no '-' in front
            return string.Concat(ID, Command);
        }
    }
}
