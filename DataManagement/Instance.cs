using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataObjects;
using FoxyBrowser716_WinUI.StaticData;

namespace FoxyBrowser716_WinUI.DataManagement;

public class Instance
{
	public readonly string InstanceName;
	public readonly string InstanceFolderPath;
	public readonly string ExtensionFolder;
	public readonly string WebView2Folder;
	public readonly string DataFolder;

	public InstanceSettings Settings;
	
	public InstanceCacheData CacheData;
	
	public readonly bool IsPrimaryInstance;

	public List<MainWindow> Windows = [];
	public MainWindow? CurrentWindow => Windows.FirstOrDefault();
	
	public event Action<Theme>? ThemeUpdated;
	private Theme _currentTheme;
	public Theme CurrentTheme
	{
		get => _currentTheme;
		set
		{
			_currentTheme = value;
			ThemeUpdated?.Invoke(value);
		}
	}
	
}