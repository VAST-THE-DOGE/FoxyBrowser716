using System.Windows;
using System.Windows.Media;

namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}

public static class ColorPalette
{
	public static Color MainColor => Color.FromRgb(30, 35, 50);
	public static Color AccentColor => Color.FromRgb(54, 64, 91);

	public static Color HighlightColor => Color.FromRgb(255, 145, 3);
}