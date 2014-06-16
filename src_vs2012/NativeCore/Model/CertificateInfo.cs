using System;
using System.Globalization;

namespace BlackBerry.NativeCore.Model
{
    /// <summary>
    /// Class describing developer's certificate.
    /// </summary>
    public sealed class CertificateInfo
    {
        private CertificateInfo()
        {
        }

        public CertificateInfo(string alias, string subjectName, string issuer, string algorithm, string fingerprintSHA1, string fingerprintMD5)
        {
            Alias = alias;
            SubjectName = subjectName;
            Issuer = issuer;
            Algorithm = algorithm;
            FingerprintSHA1 = fingerprintSHA1;
            FingerprintMD5 = fingerprintMD5;
        }

        #region Properties

        public string Alias
        {
            get;
            private set;
        }

        public string SerialNumber
        {
            get;
            private set;
        }

        public string SubjectName
        {
            get;
            private set;
        }

        public string Issuer
        {
            get;
            private set;
        }

        public DateTime ValidFrom
        {
            get;
            private set;
        }

        public DateTime ValidTo
        {
            get;
            private set;
        }

        public string Algorithm
        {
            get;
            private set;
        }

        public string PublicKey
        {
            get;
            private set;
        }

        public string FingerprintSHA1
        {
            get;
            private set;
        }

        public string FingerprintMD5
        {
            get;
            private set;
        }

        #endregion

        /// <summary>
        /// Extracts certificate info out of given text.
        /// </summary>
        public static CertificateInfo Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            /*
Found 1 private key
Found 1 certificate:
    Alias:
	author
    Serial Number:
	aa:bb:cc:dd
    Subject Name:
	CommonName=XyXyXyXyXyXy
    Issuer Name:
	CommonName=XyXyXyXyXyXy
    Valid From:
	Thu Oct 18 22:25:41 CEST 2012
    Valid To:
	Wed Oct 13 22:25:41 CEST 2032
    Public Key:
	ECC-SECP521R1
    Signature Algorithm:
	SHA512withECDSA
    SHA1 Fingerprint:
	aa:bb:cc:dd:ee:ff:gg:hh:ii:jj:kk:ll:00:11:22:33:44:55:66:77
    MD5 Fingerprint:
	00:11:22:33:44:55:66:77:88:99:00:11:22:33:44:55
             */

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // remove white chars...
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Trim();
            }

            var result = new CertificateInfo();

            // do the parsing:
            //  - assuming that n-line has header and n+1 - data
            for (int i = 0; i < lines.Length - 1; i++)
            {
                if (string.Compare("alias:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.Alias = lines[++i];
                    continue;
                }
                if (string.Compare("serial number:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.SerialNumber = lines[++i];
                    continue;
                }
                if (string.Compare("subject name:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.SubjectName = GetCommonName(lines[++i]);
                    continue;
                }
                if (string.Compare("issuer name:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.Issuer = GetCommonName(lines[++i]);
                    continue;
                }
                if (string.Compare("valid from:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.ValidFrom = ParseDate(lines[++i]);
                    continue;
                }
                if (string.Compare("valid to:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.ValidTo = ParseDate(lines[++i]);
                    continue;
                }
                if (string.Compare("signature algorithm:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.Algorithm = lines[++i];
                    continue;
                }
                if (string.Compare("public key:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.PublicKey = lines[++i];
                    continue;
                }
                if (string.Compare("md5 fingerprint:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.FingerprintMD5 = lines[++i];
                    continue;
                }
                if (string.Compare("sha1 fingerprint:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.FingerprintSHA1 = lines[++i];
                    continue;
                }
            }

            // verify if at least some basic fields are set,
            // then return the result, else treat everything as parsing failure:
            return string.IsNullOrEmpty(result.Issuer) ? null : result;
        }

        private static string GetCommonName(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            if (text.StartsWith("CommonName=", StringComparison.InvariantCultureIgnoreCase))
                return "CN=" + text.Substring(11);

            return text;
        }

        /// <summary>
        /// Parses the date out of given text.
        /// </summary>
        internal static DateTime ParseDate(string text)
        {
            DateTime date;

            return TryParseDate(text, out date) ? date : date;
        }

        /// <summary>
        /// Tries to parse the date from specified string.
        /// </summary>
        internal static bool TryParseDate(string text, out DateTime result)
        {
            string timeZoneName;

            text = RemoveTimeZone(text, out timeZoneName);
            if (string.IsNullOrEmpty(text))
            {
                result = DateTime.MinValue.ToUniversalTime();
                return false;
            }

            var culture = CultureInfo.GetCultureInfo("en-US");
            var timeZoneShift = GetTimeZoneShift(timeZoneName);

            // Thu Oct 18 22:25:41 2012
            if (!DateTime.TryParseExact(text,
                                       new[] { "ddd MMM d HH:mm:ss yyyy", "ddd MMM dd HH:mm:ss yyyy" },
                                       culture, DateTimeStyles.AssumeLocal,
                                       out result))
                return false;

            result = result.Subtract(timeZoneShift).ToUniversalTime();
            return true;
        }

        private static TimeSpan GetTimeZoneShift(string timeZoneName)
        {
            // PH: TODO: implement proper time-zone shift detection
            // like for CEST -> +2:00
            // and change the parsing code to use AssumeUniversal instead of AssumeLocal
            return new TimeSpan(0, 0, 0);
        }

        private static string RemoveTimeZone(string text, out string timeZoneName)
        {
            timeZoneName = null;
            if (string.IsNullOrEmpty(text))
                return null;

            // find text between last two spaces:
            int endIndex = text.LastIndexOf(' ');
            if (endIndex <= 0)
                return text;
            int startIndex = text.LastIndexOf(' ', endIndex - 1);
            if (startIndex < 0)
                return text;

            timeZoneName = text.Substring(startIndex + 1, endIndex - startIndex - 1);
            return string.Concat(text.Substring(0, startIndex), text.Substring(endIndex));
        }
    }
}
