using WinRT;

namespace FoxyBrowser716.DataObjects.Settings;

public abstract class ThemedUserControl : UserControl
{
	public Theme CurrentTheme { get => field; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

	protected abstract void ApplyTheme();
}