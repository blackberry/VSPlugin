using System;

namespace BlackBerry.NativeCore.QConn.Model
{
    public sealed class DataReaderBigEndian : IDataReader
    {
        private readonly QDataSource _source;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public DataReaderBigEndian(QDataSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
        }

        public int ReadInt32()
        {
            throw new NotImplementedException();
        }

        public uint ReadUInt32()
        {
            throw new NotImplementedException();
        }

        public ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public void Skip(int bytes)
        {
            throw new NotImplementedException();
        }

        public string ReadString()
        {
            throw new NotImplementedException();
        }
    }
}
