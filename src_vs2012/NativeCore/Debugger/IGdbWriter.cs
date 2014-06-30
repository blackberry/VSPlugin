namespace BlackBerry.NativeCore.Debugger
{
    public interface IGdbSender
    {
        void Break();
        void Send(string text);
    }
}
