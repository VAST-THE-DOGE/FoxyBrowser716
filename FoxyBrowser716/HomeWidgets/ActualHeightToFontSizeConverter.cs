using System;
using System.Globalization;
using System.Windows.Data;

namespace FoxyBrowser716.Converters;

public class ActualHeightToFontSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actualHeight)
        {
            return actualHeight * 0.5;
        }
        return 12.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}