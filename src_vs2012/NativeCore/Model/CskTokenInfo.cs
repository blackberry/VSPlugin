using System;
using System.IO;
using BlackBerry.NativeCore.Diagnostics;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Class describing BlackBerry ID token.
    /// </summary>
    public sealed class CskTokenInfo
    {
        private CskTokenInfo()
        {
        }

        /// <summary>
        /// Init constructor.
        /// </summary>
        public CskTokenInfo(string content)
        {
            Content = content;
            Parse(content, this);
        }

        #region Properties

        /// <summary>
        /// Gets the whole content of the token data.
        /// </summary>
        public string Content
        {
            get;
            private set;
        }

        public bool HasContent
        {
            get { return !string.IsNullOrEmpty(Content); }
        }

        public DateTime CreatedAt
        {
            get;
            private set;
        }

        public DateTime ValidTo
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets an indication, if the token is still valid.
        /// </summary>
        public bool IsValid
        {
            get { return DateTime.UtcNow < ValidTo; }
        }

        /// <summary>
        /// Gets the string representation of the creation date.
        /// </summary>
        public string CreatedAtString
        {
            get { return CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"); }
        }

        /// <summary>
        /// Gets the string representation of the validation date.
        /// </summary>
        public string ValidDateString
        {
            get { return ValidTo.ToString("yyyy-MM-dd"); }
        }

        /// <summary>
        /// Gets the number of days passed after the token validation date.
        /// </summary>
        public int ExpirationDays
        {
            get
            {
                if (IsValid)
                    return 0;
                if (ValidTo == DateTime.MinValue)
                    return 365;

                var diff = DateTime.UtcNow - ValidTo;
                return (int) diff.TotalDays;
            }
        }

        public string HMAC
        {
            get;
            private set;
        }

        public string Version
        {
            get;
            private set;
        }

        public string Token
        {
            get;
            private set;
        }

        public string Company
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Loads token info from specified file.
        /// </summary>
        public static CskTokenInfo Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            try
            {
                return Parse(File.ReadAllText(fileName));
            }
            catch (Exception ex)
            {
                TraceLog.WriteException(ex, "Unable to load file: \"{0}\"", fileName);
                return null;
            }
        }

        /// <summary>
        /// Extracts token info from a text content.
        /// </summary>
        public static CskTokenInfo Parse(string text)
        {
            return Parse(text, new CskTokenInfo());
        }

        /// <summary>
        /// Updates token info from a text content inside given result.
        /// </summary>
        private static CskTokenInfo Parse(string text, CskTokenInfo result)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // date from comment:
                if (line.Length > 0 && line[0] == '#')
                {
                    DateTime date;
                    if (CertificateInfo.TryParseDate(line.Substring(1), out date))
                    {
                        result.CreatedAt = date;
                        result.ValidTo = date.Date.AddYears(1); // BBID token is valid only for one year!
                        continue;
                    }
                }

                // HMAC:
                if (line.StartsWith("hmac=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.HMAC = line.Substring(5).Trim();
                    continue;
                }

                // version:
                if (line.StartsWith("version=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Version = line.Substring(8).Trim();
                    continue;
                }

                // token:
                if (line.StartsWith("token=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Token = line.Substring(6).Trim();
                    continue;
                }

                // company:
                if (line.StartsWith("company=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Company = line.Substring(8).Trim();
                    continue;
                }
            }

            return result;
        }
    }
}
