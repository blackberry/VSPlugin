using System;
using System.Collections.Generic;
using System.Text;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class for array-of-bytes conversions.
    /// </summary>
    static class BitHelper
    {
        /// <summary>
        /// Appends values from specified arrays one-by-one, starting at the given index.
        /// </summary>
        public static void Copy(byte[] data, int at, params byte[][] arrays)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            foreach (byte[] values in arrays)
            {
                Array.Copy(values, 0, data, at, values.Length);
                at += values.Length;
            }
        }

        /// <summary>
        /// Sets unsigned short value at specified index of an array.
        /// </summary>
        public static void BigEndian_Set(byte[] data, int at, ushort value)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 1 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            data[at] = (byte)((value >> 8) & 0xFF);
            data[at + 1] = (byte)(value & 0xFF);
        }

        /// <summary>
        /// Gets unsigned short value from specified index of an array.
        /// </summary>
        public static ushort BigEndian_ToUInt16(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 1 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (ushort)((data[at] << 8) | data[at + 1]);
        }

        /// <summary>
        /// Gets unsigned integer value from specified index of an array.
        /// </summary>
        public static uint BigEndian_ToUInt32(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 3 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (uint)((data[at] << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3]);
        }

        /// <summary>
        /// Gets unsigned long integer value from specified index of an array.
        /// </summary>
        public static ulong BigEndian_ToUInt64(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 7 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            uint high = (uint)((data[at] << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3]);
            uint low = (uint)((data[at + 4] << 24) | (data[at + 5] << 16) | (data[at + 6] << 8) | data[at + 7]);
            return ((ulong)high << 32) | low;
        }

        /// <summary>
        /// Sets unsigned short value at specified index of an array.
        /// </summary>
        public static void LittleEndian_Set(byte[] data, int at, ushort value)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 1 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            data[at] = (byte)(value & 0xFF);
            data[at + 1] = (byte)((value >> 8) & 0xFF);
        }

        /// <summary>
        /// Sets unsigned integer value at specified index of an array.
        /// </summary>
        public static void LittleEndian_Set(byte[] data, int at, uint value)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 3 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            data[at] = (byte)(value & 0xFF);
            data[at + 1] = (byte)((value >> 8) & 0xFF);
            data[at + 2] = (byte)((value >> 16) & 0xFF);
            data[at + 3] = (byte)((value >> 24) & 0xFF);
        }

        /// <summary>
        /// Gets unsigned short value from specified index of an array.
        /// </summary>
        public static ushort LittleEndian_ToUInt16(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 1 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (ushort)(data[at] | (data[at + 1] << 8));
        }

        /// <summary>
        /// Gets unsigned integer value from specified index of an array.
        /// </summary>
        public static uint LittleEndian_ToUInt32(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 3 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (uint)(data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24));
        }

        /// <summary>
        /// Gets unsigned long integer value from specified index of an array.
        /// </summary>
        public static ulong LittleEndian_ToUInt64(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 7 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            uint low = (uint)(data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24));
            uint high = (uint)(data[at + 4] | (data[at + 5] << 8) | (data[at + 6] << 16) | (data[at + 7] << 24));
            return ((ulong)high << 32) | low;
        }

        /// <summary>
        /// Converts given byte array to hex number.
        /// </summary>
        public static string Enconde(IEnumerable<byte> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            StringBuilder result = new StringBuilder();
            char[] HexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

            foreach (var v in data)
            {
                result.Append(HexChars[(v & 0xF0) >> 4]);
                result.Append(HexChars[v & 0x0F]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Allocates new array and copies there data from specified collection of chunks.
        /// The last one (lastChunk) doesn't need to be fully filled.
        /// </summary>
        public static byte[] Combine(List<byte[]> buffers, byte[] lastChunk, int lastChunkLength)
        {
            // calculate result length:
            int resultLength = lastChunkLength;
            if (buffers != null)
            {
                // or is it a special case, when abandoned buffer is the only item:
                if (buffers.Count == 1 && (lastChunk == null || lastChunkLength == 0))
                {
                    return buffers[0];
                }

                for (int i = 0; i < buffers.Count; i++)
                {
                    resultLength += buffers[i].Length;
                }
            }

            // copy data:
            var result = new byte[resultLength];
            int index = 0;

            if (buffers != null)
            {
                for (int i = 0; i < buffers.Count; i++)
                {
                    byte[] src = buffers[i];
                    Array.Copy(src, 0, result, index, src.Length);
                    index += src.Length;
                }
            }

            if (lastChunk != null)
            {
                // and copy the last chunk:
                Array.Copy(lastChunk, 0, result, index, lastChunkLength);
            }

            return result;
        }

        /// <summary>
        /// Combines two arrays into one.
        /// </summary>
        public static byte[] Combine(byte[] buffer, byte[] chunk, int chunkAt, int chunkLength)
        {
            if (buffer == null)
            {
                if (chunk == null)
                {
                    return null;
                }

                // extract only the part of the chunk:
                var result = new byte[chunkLength];
                Array.Copy(chunk, chunkAt, result, 0, chunkLength);
                return result;
            }

            if (chunk == null || chunkLength == 0)
            {
                return buffer;
            }

            var bothArrays = new byte[buffer.Length + chunkLength];
            Array.Copy(buffer, 0, bothArrays, 0, buffer.Length);
            Array.Copy(chunk, 0, bothArrays, buffer.Length, chunkLength);
            return bothArrays;
        }

        /// <summary>
        /// Combines two arrays into one.
        /// </summary>
        public static byte[] Combine(byte[] buffer, int bufferAt, int bufferLength, byte[] chunk)
        {
            if (buffer == null || bufferLength == 0)
            {
                return chunk;
            }

            if (chunk == null)
            {
                // extract only the part of the buffer:
                var result = new byte[bufferLength];
                Array.Copy(buffer, bufferAt, result, 0, bufferLength);
                return result;
            }

            var bothArrays = new byte[bufferLength + chunk.Length];
            Array.Copy(buffer, 0, bothArrays, bufferAt, bufferLength);
            Array.Copy(chunk, 0, bothArrays, bufferLength, chunk.Length);
            return bothArrays;
        }
    }
}
