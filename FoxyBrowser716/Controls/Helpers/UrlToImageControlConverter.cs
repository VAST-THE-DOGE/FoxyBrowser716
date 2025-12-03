namespace FoxyBrowser716.Controls.Helpers;

public class UrlToImageControlConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is string url)
		{
			return new Image
			{
				Source = string.IsNullOrWhiteSpace(url) 
					? new BitmapImage(new Uri("https://TODO")) //TODO
					: url.EndsWith(".svg") 
						? new SvgImageSource(new Uri(url)) 
						: new BitmapImage(new Uri(url)),
				Width = 18, Height = 18,
				Stretch = Stretch.Uniform,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
			};
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}