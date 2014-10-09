using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlackBerry.Package.ViewModels.Common
{
    /// <summary>
    /// Converter to hide empty collection.
    /// </summary>
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = (int)value;

            return count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
