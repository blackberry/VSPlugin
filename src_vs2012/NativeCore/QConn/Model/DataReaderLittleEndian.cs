using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing reading functionality of strings and numbers extracted from row byte stream of QDataSource.
    /// It uses little-endian arithmetic. Both the device and simulator are using this convention.
    /// </summary>
    sealed class DataReaderLittleEndian : IDataReader
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
                throw new EndOfStreamException("Failed to read all available data");

            if (_at >= _data.Length)
            {
                LoadNextDataBuffer();
                if (_finished)
                    throw new EndOfStreamException("Failed to read all available data");
            }
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
            VerifyData();

            var at = _at;
            _at += 8;
            return BitHelper.LittleEndian_ToUInt64(_data, at);
        }

        public byte[] ReadBytes(int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException("length");

            VerifyData();

            var result = new byte[length];

            // is the current buffer big enough to provide the data:
            if (_at + length <= _data.Length)
            {
                Array.Copy(_data, _at, result, 0, length);
                _at += length;

                return result;
            }

            // copy data from the current buffer:
            int resultAt = _data.Length - _at;
            Array.Copy(_data, _at, result, 0, resultAt);
            length -= resultAt;
            _at = _data.Length;

            do
            {
                // load next:
                LoadNextDataBuffer();
                if (_finished)
                    throw new QConnException("Unable to load requested amount of data");

                // and copy the next chunk of data:
                int toRead = (_data.Length <= length) ? _data.Length : length;
                Array.Copy(_data, 0, result, resultAt, toRead);
                resultAt += toRead;
                length -= toRead;
                _at = toRead;

            } while (length > 0);

            return result;
        }

        public void Skip(int bytes)
        {
            _at += bytes;
        }

        public string ReadString(uint maxLength, char terminatorChar)
        {
            VerifyData();

            List<byte[]> buffers = null;
            var at = _at;
            int i = _at;

            for (; maxLength > 0; i++, maxLength--)
            {
                if (i == _data.Length)
                {
                    if (buffers == null)
                        buffers = new List<byte[]>();

                    var chunk = new byte[i - at];
                    Array.Copy(_data, at, chunk, 0, i - at);
                    buffers.Add(chunk);

                    LoadNextDataBuffer();
                    i = 0;
                    at = 0;
                }

                if (_data[i] == terminatorChar)
                {
                    _at = i + 1; // also skip the terminator
                    return GetString(buffers, _data, at, i - at);
                }
            }

            _at = i;
            return GetString(buffers, _data, at, _data.Length - at);
        }

        private static string GetString(List<byte[]> buffers, byte[] lastChunk, int lastAt, int lastLength)
        {
            if (buffers == null)
            {
                return Encoding.UTF8.GetString(lastChunk, lastAt, lastLength);
            }

            Debug.Assert(lastAt == 0); // enforcing lastAt == 0!
            return Encoding.UTF8.GetString(BitHelper.Combine(buffers, lastChunk, lastLength));
        }
    }
}
