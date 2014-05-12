using System;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Descriptor of a available remotely NDK.
    /// </summary>
    internal class ApiInfo
    {
        public ApiInfo(string name, Version version)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");
            if (version == null)
                throw new ArgumentNullException("version");

            Name = name;
            Version = version;
            Level = new Version(version.Major, version.Minor);
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

        #endregion

        public override string ToString()
        {
            return string.Concat(Name, " (", Version, ")");
        }
    }
}
