namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Class describing the info about BAR file author (publisher).
    /// </summary>
    public sealed class AuthorInfo
    {
        /// <summary>
        /// Init constructor.
        /// Null values are accepted as this is something from user's input.
        /// </summary>
        public AuthorInfo(string id, string name)
        {
            ID = id;
            Name = name;
        }

        #region Properties

        public string ID
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return string.Concat(Name, " (", ID, ")");
        }
    }
}
