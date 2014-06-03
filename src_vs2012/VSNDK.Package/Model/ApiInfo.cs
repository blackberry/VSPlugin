using System;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Descriptor of a available remotely NDK.
    /// </summary>
    internal class ApiInfo : IComparable<ApiInfo>
    {
        private readonly string _description;

        public ApiInfo(string name, Version version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");

            Name = name;
            Version = version;
            Level = new Version(version.Major, version.Minor);

            var versionString = Version.ToString();
            _description = Name.IndexOf(versionString, StringComparison.Ordinal) >= 0 ? Name : string.Concat(Name, " (", versionString, ")");

            IsBeta = !string.IsNullOrEmpty(Name)
                     && (Name.IndexOf("beta", StringComparison.OrdinalIgnoreCase) >= 0 || Name.IndexOf("alpha", StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        #region Properties

        public string Name
        {
            get;
            private set;
        }

        public Version Version
        {
            get;
            private set;
        }

        public Version Level
        {
            get;
            private set;
        }

        /// <summary>
        /// Checks if it is really available on current machine.
        /// </summary>
        public virtual bool IsInstalled
        {
            get { return false; }
        }

        /// <summary>
        /// Gets an indication, if current API is in 'beta' state.
        /// </summary>
        public bool IsBeta
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other"/> parameter.Zero This object is equal to <paramref name="other"/>. Greater than zero This object is greater than <paramref name="other"/>. 
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public int CompareTo(ApiInfo other)
        {
            if (other == null)
                return 1;

            int cmp = Version.CompareTo(other.Version);
            if (cmp != 0)
                return cmp;

            return string.Compare(Name, other.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            return _description;
        }

        /// <summary>
        /// Creates an instance of the API for PlayBook.
        /// </summary>
        public static ApiInfo CreateTabletInfo()
        {
            return new ApiInfo("BlackBerry Native SDK for Tablet OS 2.1.0", new Version(2, 1, 0));
        }
    }
}
