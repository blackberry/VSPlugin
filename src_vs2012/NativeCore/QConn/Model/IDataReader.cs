namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Interface definition of the reader.
    /// </summary>
    public interface IDataReader
    {
        int ReadInt32();
        uint ReadUInt32();
        ulong ReadUInt64();

        void Skip(int bytes);
        string ReadString();
    }
}
