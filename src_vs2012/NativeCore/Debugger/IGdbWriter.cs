namespace BlackBerry.NativeCore.Debugger
{
    public interface IGdbSender
    {
        void Break();
        bool Send(string command);
    }
}
