namespace FoxyBrowser716.Controls.Helpers;

public class ColorToBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is Color color)
		{
			return new SolidColorBrush(color);
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}