using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;

namespace FoxyBrowser716_WinUI.Controls.Helpers;

public class HalfValueConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is double d and > 0)
		{
			if (parameter as string == "Point")
				return new Windows.Foundation.Point(d / 2, d / 2);
            
			if (targetType == typeof(CornerRadius))
				return new CornerRadius(d / 2);
                
			return d / 2;
		}
		return targetType == typeof(CornerRadius) ? new CornerRadius(0) : 0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}