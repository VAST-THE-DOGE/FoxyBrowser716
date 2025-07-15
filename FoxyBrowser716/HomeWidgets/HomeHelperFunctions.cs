using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxyBrowser716.Converters;

public class ActualHeightToFontSizeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double actualHeight)
        {
            return actualHeight * 0.5;
        }
        return 12.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double actual && double.TryParse((string)parameter, out var pct))
            return actual * pct;
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


public static class ControlExtensions
{
    public static BitmapSource PreviewControl(this Control control, double width=128, double height=128)
    {
        control.Width = width;
        control.Height = height;

        control.Measure(new Size(width, height));
        control.Arrange(new Rect(new Size(width, height)));

        var rtb = new RenderTargetBitmap(
            (int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(control);

        return rtb;
    }
}

