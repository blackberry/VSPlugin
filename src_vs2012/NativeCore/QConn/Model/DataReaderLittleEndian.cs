using System;
using System.IO;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    public sealed class DataReaderLittleEndian : IDataReader
    {
        private readonly QDataSource _source;
        private byte[] _data;
        private int _at;
        private bool _finished;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public DataReaderLittleEndian(QDataSource source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            _source = source;
            LoadNextDataBuffer();
        }

        private void LoadNextDataBuffer()
        {
            _finished = _source.Receive(int.MaxValue, out _data) != HResult.OK || _data.Length == 0;
            _at = 0;
        }

        private void VerifyData()
        {
            if (_finished)
                throw new EndOfStreamException("Read all available data");

            if (_at >= _data.Length)
            {
                LoadNextDataBuffer();
            }

            if (_finished)
                throw new EndOfStreamException("Read all available data");
        }

        public int ReadInt32()
        {
            VerifyData();

            var at = _at;
            _at += 4;
            return (int)BitHelper.LittleEndian_ToUInt32(_data, at);
        }

        public uint ReadUInt32()
        {
            VerifyData();

            var at = _at;
            _at += 4;
            return BitHelper.LittleEndian_ToUInt32(_data, at);
        }

        public ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

        public void Skip(int bytes)
        {
            _at += bytes;
        }

        public string ReadString()
        {
            VerifyData();

            var at = _at;
            for (int i = _at; i < _data.Length; i++)
            {
                if (_data[i] == 0)
                {
                    _at = i + 1;
                    return Encoding.UTF8.GetString(_data, at, i - at);
                }
            }

            _at = _data.Length;
            return Encoding.UTF8.GetString(_data, at, _data.Length - at);
        }
    }
}
