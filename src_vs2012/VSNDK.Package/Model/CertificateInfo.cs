using System;
using System.Globalization;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Class describing developer's certificate.
    /// </summary>
    internal sealed class CertificateInfo
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
                    result.SubjectName = lines[++i];
                    continue;
                }
                if (string.Compare("issuer name:", lines[i], StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result.Issuer = lines[++i];
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

        private static DateTime ParseDate(string text)
        {
            text = RemoveTimeZone(text);
            if (string.IsNullOrEmpty(text))
                return DateTime.MinValue.ToUniversalTime();

            var culture = CultureInfo.GetCultureInfo("en-US");

            // Thu Oct 18 22:25:41 2012
            return DateTime.ParseExact(text,
                                       new[] { "ddd MMM d HH:mm:ss yyyy", "ddd MMM dd HH:mm:ss yyyy" },
                                       culture, DateTimeStyles.AssumeLocal).ToUniversalTime();
        }

        private static string RemoveTimeZone(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // find text between last two spaces:
            int endIndex = text.LastIndexOf(' ');
            if (endIndex <= 0)
                return text;
            int startIndex = text.LastIndexOf(' ', endIndex - 1);
            if (startIndex < 0)
                return text;

            return string.Concat(text.Substring(0, startIndex), text.Substring(endIndex));
        }
    }
}
