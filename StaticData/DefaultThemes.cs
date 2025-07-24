using System.Drawing;

namespace FoxyBrowser716_WinUI.StaticData;

public class DefaultThemes
{
	public Dictionary<string, Theme> Themes = new()
	{
		["Dark Mode"] = DarkMode,
		["Light Mode"] = LightMode,
		["Foxy Theme"] = FoxyTheme,
		["Vast Theme"] = VastTheme,
	};

	public static Theme DarkMode = new()
	{
		PrimaryBackgroundColor = Color.FromArgb(255, 54, 56, 64),
		SecondaryBackgroundColor = Color.FromArgb(255, 43, 44, 51),
		PrimaryForegroundColor = Color.FromArgb(255, 255, 255, 255),
		SecondaryForegroundColor = Color.FromArgb(255, 200, 200, 200),
		PrimaryAccentColor = Color.FromArgb(255, 102, 104, 112),
		SecondaryAccentColor = Color.FromArgb(255, 66, 69, 79),
		PrimaryHighlightColor = Color.FromArgb(255, 255, 150, 0),
		SecondaryHighlightColor = Color.FromArgb(255, 200, 100, 0),
		YesColor = Color.FromArgb(255, 0, 225, 0),
		NoColor = Color.FromArgb(255, 225, 0, 0),
	};
	public static Theme LightMode = new()
	{
		PrimaryBackgroundColor = Color.FromArgb(255, 255, 255, 255),
		SecondaryBackgroundColor = Color.FromArgb(255, 225, 225, 225),
		PrimaryForegroundColor = Color.FromArgb(255, 0,0,0),
		SecondaryForegroundColor = Color.FromArgb(255, 75,75,75),
		PrimaryAccentColor = Color.FromArgb(255, 175, 175, 175),
		SecondaryAccentColor = Color.FromArgb(255, 125, 125, 125),
		PrimaryHighlightColor = Color.FromArgb(255, 255, 150, 0),
		SecondaryHighlightColor = Color.FromArgb(255, 255, 175, 0),
		YesColor = Color.FromArgb(255, 0, 225, 0),
		NoColor = Color.FromArgb(255, 225, 0, 0),
	};
	public static Theme FoxyTheme = new()
	{
		PrimaryBackgroundColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryBackgroundColor = Color.FromArgb(255, 0, 0, 0),
		PrimaryForegroundColor = Color.FromArgb(255, 255, 255, 255),
		SecondaryForegroundColor = Color.FromArgb(255, 200, 200, 200),
		PrimaryAccentColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryAccentColor = Color.FromArgb(255, 0, 0, 0),
		PrimaryHighlightColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryHighlightColor = Color.FromArgb(255, 0, 0, 0),
		YesColor = Color.FromArgb(255, 0, 225, 0),
		NoColor = Color.FromArgb(255, 225, 0, 0),
	};
	public static Theme VastTheme = new()
	{
		PrimaryBackgroundColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryBackgroundColor = Color.FromArgb(255, 0, 0, 0),
		PrimaryForegroundColor = Color.FromArgb(255, 255, 255, 255),
		SecondaryForegroundColor = Color.FromArgb(255, 200, 200, 200),
		PrimaryAccentColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryAccentColor = Color.FromArgb(255, 0, 0, 0),
		PrimaryHighlightColor = Color.FromArgb(255, 0, 0, 0),
		SecondaryHighlightColor = Color.FromArgb(255, 0, 0, 0),
		YesColor = Color.FromArgb(255, 0, 225, 0),
		NoColor = Color.FromArgb(255, 225, 0, 0),
	};
}