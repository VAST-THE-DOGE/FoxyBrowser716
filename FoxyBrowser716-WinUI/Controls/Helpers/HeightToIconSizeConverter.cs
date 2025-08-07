namespace FoxyBrowser716_WinUI.Controls.Helpers;

public class HeightToIconSizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is double height and > 0)
		{
			// Calculate icon size as a percentage of the control height
			// Subtract padding and border thickness, then take about 60-70% of remaining height
			double availableHeight = Math.Max(0, height - 8); // 2px padding + 2px border on top/bottom
			double iconSize = Math.Max(12, Math.Min(availableHeight * 0.65, 24)); // Min 12px, max 24px
			return iconSize;
		}
		return 16.0; // Default size
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotImplementedException();
	}
}
