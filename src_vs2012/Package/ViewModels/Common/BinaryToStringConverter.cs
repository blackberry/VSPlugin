using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BlackBerry.Package.ViewModels.Common
{
    /// <summary>
    /// Converter lass to exchange an array of bytes into string to display inside IDE.
    /// </summary>
    public sealed class BinaryToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = value as byte[];

            if (data != null && data.Length > 0)
            {
                const int LineWidth = 32;
                const int LineInSeparator = 8;

                var result = new StringBuilder();
                char[] hex = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
                char[] asChars = new char[LineWidth];

                for (int i = 0, j = 0; i < data.Length;)
                {
                    // print as HEX:
                    result.Append(hex[data[i] / 16]);
                    result.Append(hex[data[i] % 16]);

                    // and store as ASCII CHAR for further reference (appended at the end of line):
                    asChars[j++] = data[i] >= 32 && data[i] <= 127 ? (char) data[i] : '.';
                    i++;

                    // is it a defined 
                    if ((i % LineWidth) == 0)
                    {
                        j = 0;
                        result.Append("   ");
                        result.Append(asChars);
                        result.AppendLine();
                    }
                    else
                    {
                        if ((i % LineInSeparator) == 0)
                            result.Append("  ");
                        result.Append(' ');
                    }
                }

                return result.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
