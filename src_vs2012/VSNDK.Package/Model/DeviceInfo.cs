using System;
using System.Windows;
using System.Globalization;

namespace RIM.VSNDK_Package.Model
{
    internal sealed class DeviceInfo
    {
        public DeviceInfo()
        {
            ScreenResolution = Size.Empty;
        }

        #region Properties

        public DeviceTheme DefaultTheme
        {
            get;
            private set;
        }

        public ulong PIN
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }

        public Version SystemVersion
        {
            get;
            private set;
        }

        public string SystemName
        {
            get;
            private set;
        }

        public string ModelFamily
        {
            get;
            private set;
        }

        public string ModelFullName
        {
            get;
            private set;
        }

        public string ModelName
        {
            get;
            private set;
        }

        public string ModelNumber
        {
            get;
            private set;
        }

        public uint ScreenDPI
        {
            get;
            private set;
        }

        public Size ScreenResolution
        {
            get;
            private set;
        }

        public DeviceDebugTokenInfo DebugToken
        {
            get;
            private set;
        }

        #endregion

        public override string ToString()
        {
            return string.Concat(Name, " (", ModelName, ", OS v", SystemVersion, ")");
        }

        /// <summary>
        /// Parses device information out of given text.
        /// </summary>
        public static DeviceInfo Parse(string text, out string error)
        {
            error = null;

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var result = new DeviceInfo();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var sections = new[] { "deviceproperties", "devmode", "versions" };
            int activeSection = -1;
            int lastSection = -1;

            string dtAuthor = null;
            DateTime dtExpiryDate = DateTime.MinValue;
            bool dtValid = false;
            bool dtInstalled = false;
            string dtErrorMessage = null;
            uint dtErrorCode = 0;

            foreach (var line in lines)
            {
                // is it a runtime error description?
                if (line.StartsWith("error:", StringComparison.InvariantCultureIgnoreCase))
                {
                    // if so - further parsing is not needed
                    error = line.Substring(6).Trim();
                    break;
                }

                string name, value;
                bool isSection = SplitLine(line, out name, out value);

                // try to dected, which secition we are just entering:
                if (isSection)
                {
                    lastSection = activeSection;
                    activeSection = Array.IndexOf(sections, name);

                    if (activeSection == 1) // devmode
                    {
                        // clean up debug-token state:
                        dtAuthor = null;
                        dtExpiryDate = DateTime.MinValue;
                        dtValid = false;
                        dtInstalled = false;
                        dtErrorMessage = null;
                        dtErrorCode = 0;
                    }
                    else
                    {
                        if (lastSection == 1) // was devmode
                        {
                            result.DebugToken = new DeviceDebugTokenInfo(dtAuthor, dtExpiryDate, dtValid, dtInstalled, dtErrorMessage, dtErrorCode);
                        }
                    }

                    continue;
                }

                // parse fields within section:
                switch (activeSection)
                {
                    case 0: // deviceproperties
                        switch (name)
                        {
                            case "defaultTheme":
                                result.DefaultTheme = GetTheme(value);
                                break;
                            case "devicepin":
                                result.PIN = ParseDeviceId(value);
                                break;
                            case "device_os":
                                result.SystemName = value;

                                // unfortunatelly PlayBook doesn't return full info, as expected, so we set it manually based on specs:
                                if (string.Compare(value, "BlackBerry PlayBook OS", StringComparison.InvariantCultureIgnoreCase) == 0)
                                {
                                    if (string.IsNullOrEmpty(result.Name))
                                        result.Name = "PlayBook";
                                    if (string.IsNullOrEmpty(result.ModelFamily))
                                        result.ModelFamily = "Tablet"; // Touch ??
                                    if (string.IsNullOrEmpty(result.ModelFullName))
                                        result.ModelFullName = "BlackBerry PlayBook";
                                    if (string.IsNullOrEmpty(result.ModelName))
                                        result.ModelName = "PlayBook";
                                    if (string.IsNullOrEmpty(result.ModelNumber))
                                        result.ModelNumber = "STL100-0";
                                    if (result.ScreenDPI == 0)
                                        result.ScreenDPI = 169;
                                    if (result.ScreenResolution.IsEmpty)
                                        result.ScreenResolution = new Size(1024, 600);
                                }
                                break;
                            case "scmbundle":
                                result.SystemVersion = new Version(value);
                                break;
                            case "modelfamily":
                                result.ModelFamily = value;
                                break;
                            case "modelfullname":
                                result.ModelFullName = value;
                                break;
                            case "modelname":
                                result.ModelName = value;
                                break;
                            case "modelnumber":
                                result.ModelNumber = value;
                                break;
                            case "screen_dpi":
                                result.ScreenDPI = GetNumber(value);
                                break;
                            case "screen_res":
                                result.ScreenResolution = GetScreenResolution(value);
                                break;
                        }
                        break;
                    case 1:
                        switch (name)
                        {
                            case "debug_token_author":
                                dtAuthor = value;
                                break;
                            case "debug_token_expiration":
                                dtExpiryDate = GetDate(value);
                                break;
                            case "debug_token_installed":
                                dtInstalled = GetBool(value);
                                break;
                            case "debug_token_valid":
                                dtValid = GetBool(value);
                                break;
                            case "debug_token_validation_error":
                                dtErrorMessage = value;
                                break;
                            case "debug_token_validation_error_code":
                                dtErrorCode = GetNumber(value);
                                break;
                        }
                        break;
                    case 2: // versions
                        switch (name)
                        {
                            case "hostname":
                                result.Name = value;
                                break;
                        }
                        break;
                }
            }

            if (result.DebugToken == null)
            {
                if (lastSection == 1) // devmode
                {
                    result.DebugToken = new DeviceDebugTokenInfo(dtAuthor, dtExpiryDate, dtValid, dtInstalled, dtErrorMessage, dtErrorCode);
                }
                else
                {
                    result.DebugToken = new DeviceDebugTokenInfo(null, DateTime.MinValue, false, false, null, 0);
                }
            }

            return string.IsNullOrEmpty(error) && result.SystemVersion != null && result.PIN != 0 ? result : null;
        }

        private static DateTime GetDate(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                DateTime result;
                CultureInfo cultureInfo = CultureInfo.GetCultureInfo("en-US");
                var formats = new[] { "R", "U",
                                      "ddd MMM dd HH':'mm':'ss 'GMT'zzz yyyy",
                                      "ddd MMMM dd HH':'mm':'ss 'GMT'zzz yyyy",
                                      "ddd MMM d HH':'mm':'ss 'GMT'zzz yyyy",
                                      "ddd MMMM d HH':'mm':'ss 'GMT'zzz yyyy" };

                foreach (var style in formats)
                {
                    if (DateTime.TryParseExact(text, style, cultureInfo, DateTimeStyles.None, out result))
                        return result.ToUniversalTime();
                }
            }

            return DateTime.MinValue.ToUniversalTime();
        }

        private static bool GetBool(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            if (string.Compare(text, "true", StringComparison.InvariantCultureIgnoreCase) == 0)
                return true;
            if (string.CompareOrdinal(text, "1") == 0)
                return true;
            if (string.Compare(text, "false", StringComparison.InvariantCultureIgnoreCase) == 0)
                return false;
            if (string.CompareOrdinal(text, "0") == 0)
                return false;

            throw new FormatException("Invalid boolean value");
        }

        private static Size GetScreenResolution(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Size.Empty;

            int separatorIndex = text.IndexOf('x');
            if (separatorIndex < 0)
                return Size.Empty;

            int width;
            int height;

            if (!int.TryParse(text.Substring(0, separatorIndex), out width))
                width = 0;
            if (!int.TryParse(text.Substring(separatorIndex + 1), out height))
                height = 0;

            return new Size(width, height);
        }

        private static uint GetNumber(string text)
        {
            uint dpi;
            return uint.TryParse(text, out dpi) ? dpi : 0;
        }

        /// <summary>
        /// Converts string representation of PIN into a number.
        /// </summary>
        internal static ulong ParseDeviceId(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            ulong id;

            if (text.StartsWith("0x") || text.StartsWith("0X"))
            {
                return ulong.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier | NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingWhite, null, out id) ? id : 0;
            }

            return ulong.TryParse(text, NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingWhite, null, out id) ? id : 0;
        }

        private static DeviceTheme GetTheme(string text)
        {
            if (string.IsNullOrEmpty(text))
                return DeviceTheme.Unknown;
            if (string.Compare(text, "white", StringComparison.InvariantCultureIgnoreCase) == 0)
                return DeviceTheme.White;
            if (string.Compare(text, "black", StringComparison.InvariantCultureIgnoreCase) == 0)
                return DeviceTheme.Black;
            
            return DeviceTheme.Unknown;
        }

        private static bool SplitLine(string line, out string name, out string value)
        {
            if (string.IsNullOrEmpty(line))
            {
                name = null;
                value = null;
                return false;
            }

            // is it a section startup marker?
            if (line.StartsWith("@") || line.StartsWith("[n]@"))
            {
                name = line.Substring(line.IndexOf('@') + 1).Trim();
                value = null;
                return true;
            }

            // Split the typical PPS field definition of format: <name>:<type>:<value>
            // where type part is optional
            // and also be aware of some random messages in format: <name>:<value>
            int typeStartAt = line.IndexOf(':');
            if (typeStartAt >= 0)
            {
                int valueStartAt = line.IndexOf(':', typeStartAt + 1);
                if (valueStartAt < 0)
                {
                    name = line.Substring(0, typeStartAt);
                    value = line.Substring(typeStartAt + 1).Trim();
                }
                else
                {
                    name = line.Substring(0, typeStartAt);
                    value = line.Substring(valueStartAt + 1).Trim();
                }

                // remove persistance option qualifier from its name:
                if (name.StartsWith("[n]"))
                {
                    name = name.Substring(3);
                }

                if (name.Length == 0)
                {
                    name = null;
                }
                if (value.Length == 0)
                {
                    value = null;
                }
            }
            else
            {
                name = null;
                value = null;
            }

            return false;
        }
    }
}
