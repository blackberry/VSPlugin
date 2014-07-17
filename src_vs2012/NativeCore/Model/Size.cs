using System;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Class describing 2-dimensional space.
    /// </summary>
    public sealed class Size : IEquatable<Size>
    {
        public static readonly Size Empty = new Size(0, 0);

        /// <summary>
        /// Init constructor.
        /// </summary>
        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        #region Properties

        /// <summary>
        /// Gets the width value.
        /// </summary>
        public uint Width
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the height value.
        /// </summary>
        public uint Height
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks, wheather there is any dimension defined.
        /// </summary>
        public bool IsEmpty
        {
            get { return Width == 0 && Height == 0; }
        }

        #endregion

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Width * 397) ^ (int)Height;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Size && Equals((Size) obj);
        }

        public bool Equals(Size other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Width == other.Width && Height == other.Height;
        }

        public override string ToString()
        {
            return string.Concat(Width, "x", Height);
        }
    }
}
