using System.Windows.Media;

namespace FoxyBrowser716.Styling;

public static class ColorPalette
{
    public static Color Transparent => Color.FromArgb(0, 0, 0, 0);
    public static Color MainColor => Color.FromRgb(30, 35, 50);
    public static Color AccentColor => Color.FromRgb(54, 64, 91);
    public static Color HighlightColor => Color.FromRgb(255, 145, 3);
    
    //TODO: revamp this above stuff and add all the colors I could ever need so that everything is consistent
    
    public static Color YesColor => Color.FromRgb(0,200,0);
    public static Color NoColor => Color.FromRgb(200,0,0);
    public static Color YesColorTransparent => Color.FromArgb(150, 0,200,0);
    public static Color NoColorTransparent => Color.FromArgb(150, 200,0,0);
}
