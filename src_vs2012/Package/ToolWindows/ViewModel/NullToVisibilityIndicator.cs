using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlackBerry.Package.ToolWindows.ViewModel
{
    /// <summary>
    /// Converter class to help hide UI items with 'null' value.
    /// </summary>
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
