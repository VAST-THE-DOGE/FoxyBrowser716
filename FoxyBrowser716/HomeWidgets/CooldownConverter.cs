using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FoxyBrowser716.HomeWidgets;

public class CooldownConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is double val)
		{
			return val > 50 ? Brushes.Blue : Brushes.Red; // Example: Blue when available, Red when on cooldown
		}
		return Brushes.Gray;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}