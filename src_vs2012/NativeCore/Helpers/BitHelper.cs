using System;

namespace BlackBerry.NativeCore.Helpers
{
    /// <summary>
    /// Helper class for array-of-bytes conversions.
    /// </summary>
    static class BitHelper
    {
        /// <summary>
        /// Sets unsigned short value at specified index of an array.
        /// </summary>
        public static void Set(byte[] data, int at, ushort value)
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
        public static ushort GetUInt16(byte[] data, int at)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (at + 1 >= data.Length || at < 0)
                throw new ArgumentOutOfRangeException("at");

            return (ushort)((data[at] << 8) | data[at + 1]);
        }
    }
}
