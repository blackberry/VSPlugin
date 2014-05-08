using System;

namespace RIM.VSNDK_Package.Model
{
    internal sealed class DebugTokenInfo
    {
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

        public string Author
        {
            get;
            private set;
        }

        public string AuthorID
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
        /// Creates new instance of the DebugTokenInfo based on specified data, or null if data is invalid.
        /// </summary>
        public static DebugTokenInfo Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var items = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            DebugTokenInfo result = new DebugTokenInfo();
            bool isDebugToken = false;

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
                            result.Author = value;
                            break;
                        case "package-author-id":
                            result.AuthorID = value;
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

            return !isDebugToken || string.IsNullOrEmpty(result.ID) || string.IsNullOrEmpty(result.Author) ? null : result;
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
                deviceIDs[i] = ParseDeviceId(items[i]);
            }

            return deviceIDs;
        }

        private static ulong ParseDeviceId(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            ulong id;
            return ulong.TryParse(text, out id) ? id : 0;
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
