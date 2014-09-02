using System;
using System.Text;
using BlackBerry.NativeCore.Helpers;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing information about a target path.
    /// </summary>
    public class TargetFile : IComparable<TargetFile>
    {
        internal const int ModeOpenNone = 0;
        internal const int ModeOpenReadOnly = 1;
        private const int ModeOpenWriteOnly = 2;
        internal const int ModeOpenReadWrite = ModeOpenReadOnly | ModeOpenWriteOnly;

        internal const uint TypeMask = 0xFFFF0FFF;
        private const uint TypeCharacterDevice = 0x2000;
        internal const uint TypeDirectory = 0x4000;
        private const uint TypeNamedPipe = 0x5000;
        private const uint TypeBlockDevice = 0x6000;
        private const uint TypeRegularFile = 0x8000;
        private const uint TypeSymlink = 0xA000;
        private const uint TypeSocket = 0xC000;

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetFile(uint mode, ulong size, uint flags, string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            Mode = mode;
            Size = size;
            Flags = flags;
            CreationTime = DateTime.MinValue;
            Path = path;
            Name = PathHelper.ExtractName(path);
            UpdateFormatting();
            UpdateAccess();
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public TargetFile(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            CreationTime = DateTime.MinValue;
            NoAccess = true;
            Path = path;
            Name = string.IsNullOrEmpty(name) ? path : name;
            UpdateFormatting();
        }

        #region Properties

        public uint Mode
        {
            get;
            private set;
        }

        public uint Permissions
        {
            get { return Mode & 0xFFF; }
        }

        public uint Type
        {
            get { return Mode & 0xF000; }
        }

        public string FormattedPermissions
        {
            get;
            private set;
        }

        public char FormattedType
        {
            get;
            private set;
        }

        public ulong Size
        {
            get;
            private set;
        }

        public uint Flags
        {
            get;
            private set;
        }

        public uint UserID
        {
            get;
            private set;
        }

        public uint GroupID
        {
            get;
            private set;
        }

        public DateTime CreationTime
        {
            get;
            private set;
        }

        public string Path
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public bool IsFile
        {
            get { return Type == TypeRegularFile; }
        }

        public bool IsDirectory
        {
            get { return Type == TypeDirectory; }
        }

        public bool IsSymlink
        {
            get { return Type == TypeSymlink; }
        }

        /// <summary>
        /// Gets an indication, if specified object describes 'something', we had no access to.
        /// That's why it was impossible to load info and all properties are invalid.
        /// </summary>
        public bool NoAccess
        {
            get;
            private set;
        }

        public bool CanRead
        {
            get { return (Flags & ModeOpenReadOnly) == ModeOpenReadOnly; }
        }

        public bool CanWrite
        {
            get { return (Flags & ModeOpenWriteOnly) == ModeOpenWriteOnly; }
        }

        #endregion

        #region IComparable Implementation

        public int CompareTo(TargetFile other)
        {
            if (other == null)
                return 1;

            // non-folders should be first on the list:
            if (!IsDirectory)
            {
                if (!other.IsDirectory)
                    return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
                return -1;
            }

            if (IsDirectory && !other.IsDirectory)
                return 1;

            return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
        }

        #endregion

        public override string ToString()
        {
            return string.Concat(Path, " (", FormattedPermissions, ")");
        }

        internal void Update(uint userID, uint groupID, DateTime creationTime, uint mode, ulong size)
        {
            UserID = userID;
            GroupID = groupID;
            CreationTime = creationTime;
            Mode = mode;
            Size = size;
            UpdateFormatting();
            UpdateAccess();
        }

        internal void Update(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Name = name;
            }
        }

        private void UpdateFormatting()
        {
            if (NoAccess)
            {
                FormattedPermissions = "N/A";
                FormattedType = '-';
            }
            else
            {
                FormattedType = GetTypeLetter(Type);

                StringBuilder result = new StringBuilder();

                // append type:
                result.Append(FormattedType);

                // append permissions (user/group/all):
                const string AllPermissions = "xwr";
                uint permissions = Permissions;
                int permissionIndex = 0;

                for (int i = 0; i < 9; i++)
                {
                    result.Insert(1, (permissions & 1) == 1 ? AllPermissions[permissionIndex] : '-');

                    permissions >>= 1;
                    permissionIndex = (permissionIndex + 1) % AllPermissions.Length;
                }

                FormattedPermissions = result.ToString();
            }
        }

        private static char GetTypeLetter(uint type)
        {
            switch (type)
            {
                case TypeCharacterDevice:
                    return 'c';
                case TypeDirectory:
                    return 'd';
                case TypeNamedPipe:
                    return 'p';
                case TypeBlockDevice:
                    return 'b';
                case TypeRegularFile:
                    return '-';
                case TypeSymlink:
                    return 'l';
                case TypeSocket:
                    return 's';
                default:
                    throw new ArgumentOutOfRangeException("type", "Unrecognized file type");
            }
        }

        private void UpdateAccess()
        {
            NoAccess = NoAccess || (Type == TypeCharacterDevice);
        }

        #region Path Management

        /// <summary>
        /// Creates a path to item with a specified name (file or folder).
        /// </summary>
        public string CreateItemPath(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return PathHelper.MakePath(Path, name);
        }

        #endregion
    }
}
