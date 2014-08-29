namespace BlackBerry.NativeCore.Debugger.Requests
{
    /// <summary>
    /// Class wrapping the regular MI-command send to the GDB.
    /// </summary>
    public class MiRequest : Request
    {
        public MiRequest(string command)
            : base(command)
        {
        }

        public override string ToString()
        {
            // MI commands to GDB have this magic '-' in front of them
            return string.Concat(ID, "-", Command);
        }
    }
}
