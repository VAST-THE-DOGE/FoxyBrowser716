using FoxyBrowser716.ErrorHandeler;
using Material.Icons.WinUI3;

namespace FoxyBrowser716.Controls.Helpers;

public partial class UrlToImageControlConverter : IValueConverter
{
	public static UIElement StaticConvert(object value)
	{
		if (value is string { Length: > 0 } url)
		{
			try
			{
				return new Image
				{
					Source = url.EndsWith(".svg") 
						? new SvgImageSource(new Uri(url)) 
						: new BitmapImage(new Uri(url)),
					Width = 18, Height = 18,
					Stretch = Stretch.Uniform,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				};
			}
			catch (Exception e)
			{
				FoxyLogger.AddError(e);
				
			}
		}

		return new MaterialIcon
		{
			Kind = MaterialIconKind.Web, 
			Foreground = new SolidColorBrush(Colors.Gray)
		};
	}
	
	public object Convert(object value, Type targetType, object parameter, string language)
		=> StaticConvert(value);

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotImplementedException();
}