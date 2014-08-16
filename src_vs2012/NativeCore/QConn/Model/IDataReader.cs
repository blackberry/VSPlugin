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
        byte[] ReadBytes(int length);

        void Skip(int bytes);
        string ReadString(char terminatorChar);
    }
}
