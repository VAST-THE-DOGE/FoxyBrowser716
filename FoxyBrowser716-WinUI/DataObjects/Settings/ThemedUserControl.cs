using WinRT;

namespace FoxyBrowser716_WinUI.DataObjects.Settings;

public abstract class ThemedUserControl : UserControl
{
	public Theme CurrentTheme { get => field; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

	protected abstract void ApplyTheme();
}