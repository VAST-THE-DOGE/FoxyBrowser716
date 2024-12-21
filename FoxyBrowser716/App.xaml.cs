using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;
using Color = System.Drawing.Color;

namespace FoxyBrowser716;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
}

public static class ColorPalette
{
	public static readonly SolidColorBrush MainBackgroundBrushColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 35, 50));
	public static System.Windows.Media.Color MainColor => System.Windows.Media.Color.FromRgb(30, 35, 50);
	public static System.Windows.Media.Color AccentColor => System.Windows.Media.Color.FromRgb(54, 64, 91);
	
	public static System.Windows.Media.Color HighlightColor => System.Windows.Media.Color.FromRgb(255, 145, 3);

}