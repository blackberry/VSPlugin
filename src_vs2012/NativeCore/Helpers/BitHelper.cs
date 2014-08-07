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
        public static ushort BigEndian_ToUInt32(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 4 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (ushort)((data[at] << 24) | (data[at + 1] << 16) | (data[at + 2] << 8) | data[at + 3]);
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
            if (at + 4 >= data.Length || at < 0)
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
        public static ushort LittleEndian_ToUInt32(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 4 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (ushort)(data[at] | (data[at + 1] << 8) | (data[at + 2] << 16) | (data[at + 3] << 24));
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

    }
}
