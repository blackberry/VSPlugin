using System;
using System.Collections.Generic;

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
        /// Gets the optional description of this API Level.
        /// </summary>
        public string Details
        {
            get;
            protected set;
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
            return new ApiInfo("BlackBerry Native SDK for Tablet OS 2.1.0", new Version(2, 1, 0, 1032));
        }

        /// <summary>
        /// Returns an index of NdkInfo inside a collection that has the same version.
        /// </summary>
        public static int IndexOf(IEnumerable<ApiInfo> list, Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version");

            if (list != null)
            {
                int i = 0;
                foreach (var item in list)
                {
                    if (item.Version == version)
                        return i;
                    i++;
                }
            }

            return -1;
        }

        /// <summary>
        /// Extracts the 'version' fragment out of the given directory name. Or returns 'null' in case of any problems.
        /// </summary>
        protected static Version GetVersionFromFolderName(string directoryName)
        {
            if (string.IsNullOrEmpty(directoryName))
                return null;

            int i = directoryName.Length;

            // find the version substring at the end of the name:
            while (i > 0 && directoryName[i - 1] == '_' || directoryName[i - 1] == '.' || char.IsDigit(directoryName[i - 1]))
                i--;

            // we might read one char too much:
            if (i < directoryName.Length && !char.IsDigit(directoryName[i]))
                i++;

            // parse:
            var versionString = directoryName.Substring(i).Trim().Replace('_', '.');
            if (string.IsNullOrEmpty(versionString) || versionString.IndexOf('.') < 0)
                return null;

            return new Version(versionString);
        }
    }
}
