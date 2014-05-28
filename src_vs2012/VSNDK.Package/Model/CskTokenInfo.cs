using System;
using System.IO;
using RIM.VSNDK_Package.Diagnostics;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Class describing BlackBerry ID token.
    /// </summary>
    internal sealed class CskTokenInfo
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

        public DateTime CreatedAt
        {
            get;
            private set;
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
                    }
                }

                // HMAC:
                if (line.StartsWith("hmac=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.HMAC = line.Substring(5).Trim();
                }

                // version:
                if (line.StartsWith("version=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Version = line.Substring(8).Trim();
                }

                // token:
                if (line.StartsWith("token=", StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Token = line.Substring(6).Trim();
                }
            }

            return result;
        }
    }
}
