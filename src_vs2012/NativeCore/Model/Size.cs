namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Class describing 2-dimensional space.
    /// </summary>
    internal sealed class Size
    {
        public Size(uint width, uint height)
        {
            Width = width;
            Height = height;
        }

        #region Properties

        public uint Width
        {
            get;
            private set;
        }

        public uint Height
        {
            get;
            private set;
        }
        
        #endregion

        public override string ToString()
        {
            return string.Concat(Width, "x", Height);
        }
    }
}
