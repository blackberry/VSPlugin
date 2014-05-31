using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Xml;

namespace RIM.VSNDK_Package.Model
{
    /// <summary>
    /// Detailed info about a physical device received from BlackBerry tools and services.
    /// </summary>
    internal sealed class DeviceInfo
    {
        public DeviceInfo()
        {
            ScreenResolution = Size.Empty;
            IconResolution = Size.Empty;
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

        public Size IconResolution
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
            if (string.IsNullOrEmpty(ModelName))
            {
                if (SystemVersion == null)
                    return Name;

                return string.Concat(Name, " ( OS v", SystemVersion, ")");
            }

            return string.Concat(Name, " (", ModelName, ", OS v", SystemVersion, ")");
        }

        /// <summary>
        /// Gets the values of all fields.
        /// </summary>
        public string ToLongDescription()
        {
            var result = new StringBuilder();

            result.Append("Name: ").AppendLine(Name);
            result.Append("PIN: ").AppendLine(PIN.ToString("X"));
            result.Append("Model: ").Append(ModelName).Append(" (").Append(ModelNumber).AppendLine(")");
            result.Append("Model Family: ").AppendLine(ModelFamily);
            result.Append("System: ").Append(SystemName).Append(" (").Append(SystemVersion).AppendLine(")");
            result.Append("Resolution: ").Append((uint) ScreenResolution.Width).Append("x").Append((uint) ScreenResolution.Height).Append(" (").Append(ScreenDPI).AppendLine("dpi)");
            result.Append("Theme: ").Append(DefaultTheme.ToString());

            return result.ToString();
        }

        /// <summary>
        /// Parses device information out of given PPS text.
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
                                        result.ModelNumber = "STL100-0"; // copied from Z10
                                    if (result.ScreenDPI == 0)
                                        result.ScreenDPI = 167;
                                    if (result.ScreenResolution.IsEmpty)
                                        result.ScreenResolution = new Size(1024, 600);
                                    if (result.IconResolution.IsEmpty)
                                        result.IconResolution = new Size(90, 90); // copied from Z10/Z30/Q10
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
                                result.ScreenResolution = GetResolution(value);
                                break;
                            case "icon_res":
                                result.IconResolution = GetResolution(value);
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

            // PH: no reason, but sometimes boolean field is presented as JSON (especially debug-token-valid field)...
            if (string.CompareOrdinal(text, "{}") == 0)
                return false;

            throw new FormatException("Invalid boolean value");
        }

        private static Size GetResolution(string text)
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

        /// <summary>
        /// Loads info about devices from XML file, given by the reader.
        /// It supports only one format - the one from NDK descriptor.
        /// </summary>
        public static DeviceInfo[] Load(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            var result = new List<DeviceInfo>();
            DeviceInfo info = null;
            bool isAboutIcon = false;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "device":
                            info = new DeviceInfo();
                            isAboutIcon = false;
                            break;
                        case "name":
                            if (info != null)
                                info.ModelFullName = info.Name = reader.ReadString();
                            break;
                        case "kind":
                            if (info != null)
                                info.ModelFamily = reader.ReadString();
                            break;
                        case "ppi":
                            if (info != null)
                                info.ScreenDPI = GetNumber(reader.ReadString());
                            break;
                        case "icon":
                            if (info != null)
                            {
                                isAboutIcon = true;
                            }
                            break;
                        case "width":
                            if (info != null)
                            {
                                if (isAboutIcon)
                                    info.IconResolution = UpdateWidth(reader.ReadString(), info.IconResolution.Height);
                                else
                                    info.ScreenResolution = UpdateWidth(reader.ReadString(), info.ScreenResolution.Height);
                            }
                            break;
                        case "height":
                            if (info != null)
                            {
                                if (isAboutIcon)
                                    info.IconResolution = UpdateHeight(info.IconResolution.Width, reader.ReadString());
                                else
                                    info.ScreenResolution = UpdateHeight(info.ScreenResolution.Width, reader.ReadString());
                            }
                            break;

                    }
                }

                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    // ok, got full info about the device:
                    if (reader.Name == "device")
                    {
                        if (info != null && !string.IsNullOrEmpty(info.Name))
                        {
                            result.Add(info);
                            info = null;
                        }

                        continue;
                    }

                    if (reader.Name == "icon")
                    {
                        isAboutIcon = false;
                        continue;
                    }

                    // interesting section has been read, no need to parse further:
                    if (reader.Name == "devices")
                        break;
                }
            }

            return result.ToArray();
        }

        private static Size UpdateWidth(string width, double height)
        {
            var value = GetNumber(width);

            return new Size(value, height < 0 ? 0 : height);
        }

        private static Size UpdateHeight(double width, string height)
        {
            var value = GetNumber(height);

            return new Size(width < 0 ? 0 : width, value);
        }
    }
}
