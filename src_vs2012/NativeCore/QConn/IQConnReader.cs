using BlackBerry.NativeCore.QConn.Model;

namespace BlackBerry.NativeCore.QConn
{
    public interface IQConnReader
    {
        void Select(string serviceName);
        IDataReader Send(string command);
    }
}
