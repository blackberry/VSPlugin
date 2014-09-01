using System;
using System.Text;

namespace BlackBerry.NativeCore.QConn.Model
{
    /// <summary>
    /// Class providing information about a target path.
    /// </summary>
    public class TargetFile
    {
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
        public TargetFile(uint mode, ulong size, uint flags, string path, string originalPath)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (string.IsNullOrEmpty(originalPath))
                throw new ArgumentNullException("originalPath");

            Mode = mode;
            Size = size;
            Flags = flags;
            CreationTime = DateTime.MinValue;
            Path = path;
            OriginalPath = originalPath;
            UpdateFormatting();
        }

        public TargetFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");

            CreationTime = DateTime.MinValue;
            NoAccess = true;
            Path = path;
            OriginalPath = path;
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

        public string OriginalPath
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

        #region Path Management

        /// <summary>
        /// Creates a path to item with a specified name (file or folder).
        /// </summary>
        public string CreateItemPath(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return MakePath(Path, name);
        }

        private static string MakePath(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (path[path.Length - 1] == '/')
            {
                if (name[0] == '/')
                    return path + name.Substring(1);
                return path + name;
            }

            if (name[0] == '/')
                return path + name;
            return string.Concat(path, "/", name);
        }

        #endregion
    }
}
