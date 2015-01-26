using System;
using System.Text;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Model data describing info about debug token.
    /// </summary>
    public sealed class DebugTokenInfo
    {
        private string _description;

        public DebugTokenInfo()
        {
            SystemActions = new string[0];
            Devices = new ulong[0];
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

        public Version Version
        {
            get;
            private set;
        }

        public string VersionID
        {
            get;
            private set;
        }

        public AuthorInfo Author
        {
            get;
            private set;
        }

        public string AuthorCertificateHash
        {
            get;
            private set;
        }

        public string[] SystemActions
        {
            get;
            private set;
        }

        public ulong[] Devices
        {
            get;
            private set;
        }

        public DateTime IssueDate
        {
            get;
            private set;
        }

        public DateTime ExpiryDate
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Checks, whether this debug token is build against specified device.
        /// </summary>
        public bool Contains(ulong device)
        {
            if (device == 0)
                return false;
            if (Devices == null || Devices.Length == 0)
                return false;

            for (int i = 0; i < Devices.Length; i++)
            {
                if (device == Devices[i])
                    return true;
            }

            return false;
        }


        public override string ToString()
        {
            if (_description == null)
                _description = GetDescription();

            return _description;
        }

        /// <summary>
        /// Gets the long description summary of the debug token.
        /// </summary>
        public string ToLongDescription(bool mostImportantOnly)
        {
            var result = new StringBuilder();

            if (!mostImportantOnly)
            {
                result.Append("Name: ").Append(Name).Append(" (").Append(ID).AppendLine(")");
                if (Author != null)
                {
                    result.Append("Author: ").AppendLine(Author.Name);
                }
            }

            // print dates:
            if (ExpiryDate != DateTime.MinValue)
                result.Append("Expiry Date: ").AppendLine(ExpiryDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            if (!mostImportantOnly)
            {
                if (IssueDate != DateTime.MinValue)
                    result.Append("Issue Date: ").AppendLine(IssueDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                result.Append("Version: ").Append(Version).Append(" (ID: ").Append(VersionID).AppendLine(")");
            }

            // print affected devices:
            if (mostImportantOnly && Devices.Length > 0)
            {
                result.Append("Devices: ");
                int i = 0;
                foreach (var device in Devices)
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append(device.ToString("X"));
                    i++;
                }
                result.AppendLine();
            }

            if (mostImportantOnly)
            {
                if (Author != null)
                {
                    result.Append("Author: ").AppendLine(Author.Name);
                    result.Append("Author ID: ").AppendLine(Author.ID);
                }

                result.AppendLine();
                result.Append("App ID: ").AppendLine(ID);
                result.Append("App Name: ").AppendLine(Name);
                result.Append("App Version: ").Append(Version).Append(" (").Append(VersionID).AppendLine(")");
            }

            // print system actions allowed:
            if (mostImportantOnly && SystemActions.Length > 0)
            {
                result.AppendLine();
                result.Append("Actions: ");
                
                int i = 0;
                foreach (var action in SystemActions)
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append(action);
                    i++;
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        private string GetDescription()
        {
            var result = new StringBuilder();

            if (Author != null)
            {
                result.Append(!string.IsNullOrEmpty(Author.Name) ? Author.Name : Author.ID);
            }
            result.Append(" (");
            result.Append(ExpiryDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            // serialize list of device PINs as hexadecimal values:
            if (Devices.Length > 0)
            {
                result.Append(", ");
                for (int i = 0; i < Devices.Length; i++)
                {
                    if (i > 0)
                        result.Append(",");
                    result.Append(Devices[i].ToString("X"));
                }
            }

            result.Append(")");
            return result.ToString();
        }

        /// <summary>
        /// Creates new instance of the DebugTokenInfo based on specified data, or null if data is invalid.
        /// </summary>
        public static DebugTokenInfo Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var items = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            DebugTokenInfo result = new DebugTokenInfo();
            bool isDebugToken = false;
            string authorName = null;
            string authorID = null;

            foreach (var line in items)
            {
                string key;
                string value;

                if (SplitLine(line, out key, out value))
                {
                    switch (key)
                    {
                        case "package-id":
                            result.ID = value;
                            break;
                        case "package-type":
                            isDebugToken = string.CompareOrdinal("debug-token", value) == 0;
                            break;
                        case "package-name":
                            result.Name = value;
                            break;
                        case "package-version":
                            result.Version = new Version(value);
                            break;
                        case "package-version-id":
                            result.VersionID = value;
                            break;
                        case "package-author":
                            authorName = value;
                            break;
                        case "package-author-id":
                            authorID = value;
                            break;
                        case "package-author-certificate-hash":
                            result.AuthorCertificateHash = value;
                            break;
                        case "debug-token-system-actions":
                            result.SystemActions = ParseSystemActions(value);
                            break;
                        case "debug-token-device-id":
                            result.Devices = ParseDevices(value);
                            break;
                        case "package-issue-date":
                            result.IssueDate = ParseDate(value);
                            break;
                        case "debug-token-expiry-date":
                            result.ExpiryDate = ParseDate(value);
                            break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(authorID) || !string.IsNullOrEmpty(authorName))
            {
                result.Author = new AuthorInfo(authorID, authorName);
            }

            return !isDebugToken || string.IsNullOrEmpty(result.ID) || result.Author == null ? null : result;
        }

        private static string[] ParseSystemActions(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[0];

            // convert comma-separated items into an array of strings:
            var items = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = items[i].Trim();
            }

            return items;
        }

        private static ulong[] ParseDevices(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new ulong[0];

            // convert comma-separated items into an array of numbers:
            var items = text.Split(new[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var deviceIDs = new ulong[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                deviceIDs[i] = DeviceInfo.ParseDeviceId(items[i]);
            }

            Array.Sort(deviceIDs);
            return deviceIDs;
        }

        private static DateTime ParseDate(string text)
        {
            DateTime result;

            if (!string.IsNullOrEmpty(text) && DateTime.TryParse(text, out result))
                return result.ToUniversalTime();

            return DateTime.MinValue.ToUniversalTime();
        }

        private static bool SplitLine(string line, out string key, out string value)
        {
            if (string.IsNullOrEmpty(line))
            {
                key = null;
                value = null;
                return false;
            }

            // check if following format: <key>: <value>
            int separatorIndex = line.IndexOf(": ", StringComparison.CurrentCulture);
            if (separatorIndex < 0)
            {
                key = null;
                value = null;
                return false;
            }

            key = line.Substring(0, separatorIndex).Trim().ToLower();
            value = line.Substring(separatorIndex + 2).Trim();
            return true;
        }
    }
}
