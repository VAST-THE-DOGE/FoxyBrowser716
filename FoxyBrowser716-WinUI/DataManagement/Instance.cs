using System.ComponentModel;
using Windows.Foundation;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataObjects;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using FoxyBrowser716_WinUI.DataObjects.Settings;
using FoxyBrowser716_WinUI.ErrorHandeler;
using FoxyBrowser716_WinUI.StaticData;
using WinUIEx;

namespace FoxyBrowser716_WinUI.DataManagement;

public class Instance
{
	public string Name { get; private set; }
	
	//TODO:
	public DefaultThemes DefaultThemeObject { get; } = new();
	
	public BrowserSettings Settings => _settings.Item;
	public InstanceCache Cache => _cache.Item;
	private FoxyAutoSaverField<BrowserSettings> _settings;
	private FoxyAutoSaverField<InstanceCache> _cache;
	
	public ObservableCollection<WebsiteInfo> Pins => _pins.Items;
	public ObservableCollection<WebsiteInfo> Bookmarks => _bookmarks.Items;
	private FoxyAutoSaverList<WebsiteInfo> _pins;
	private FoxyAutoSaverList<WebsiteInfo> _bookmarks;
	
	public bool IsPrimaryInstance => Name == AppServer.PrimaryInstance.Name;

	public LinkedList<MainWindow> Windows = [];
	public MainWindow? CurrentWindow => Windows.FirstOrDefault();
	
	public event Action<Theme>? ThemeUpdated;

	public Theme CurrentTheme
	{
		get;
		set
		{
			field = value;
			foreach (var window in Windows)
				window.CurrentTheme = value;
		}
	} = DefaultThemes.DarkMode;

	public event Action<Instance>? Focused;
	
	private Instance()
	{}

	private void HandleSettingsChange(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
	{
		switch (propertyChangedEventArgs.PropertyName)
		{
			case nameof(BrowserSettings.ThemeName):
				if (DefaultThemeObject.Themes.TryGetValue(Settings.ThemeName, out var theme)) CurrentTheme = theme;
				break;
			default:
				break;
		}
	}
	
	public static async Task<Instance> Create(string name, InstanceRetoreData? retoreData = null)
	{
		var newInstance = new Instance();
		await newInstance.Initialize(name, retoreData);
		return newInstance;
	}

	public async Task Initialize(string name, InstanceRetoreData? retoreData = null)
	{
		var isNewInstance = false;
		
		Name = name;
		
		_cache = new(() => new InstanceCache(), "Cache.json", FoxyFileManager.FolderType.Data, Name);
		_settings = new(() => new BrowserSettings(), "Settings.json", FoxyFileManager.FolderType.Data, Name, SavePriority.High);

		
		_pins = new("Pins.json", FoxyFileManager.FolderType.Data, Name);
		_bookmarks = new("Bookmarks.json", FoxyFileManager.FolderType.Data, Name);
			
		await AppServer.AutoSaver.AddItems([
			_settings,
			_cache,
			_pins,
			_bookmarks
		]);
		
		_settings.Item.PropertyChanged += HandleSettingsChange;
		if (DefaultThemeObject.Themes.TryGetValue(Settings.ThemeName, out var theme)) CurrentTheme = theme;
	}
	
	public async Task<MainWindow> CreateWindow(string[]? urls = null, Rect? startLocation = null, MainWindow.BrowserWindowState windowState = MainWindow.BrowserWindowState.Normal)
	{
		try
		{
			var newWindow = await MainWindow.Create(this);

			Focused?.Invoke(this);

			if (startLocation is { } sl)
			{
				newWindow.MoveAndResize(sl.X, sl.Y, sl.Width, sl.Height);
				;
			}

			newWindow.ApplyWindowState(windowState);

			Windows.AddFirst(newWindow);
			newWindow.Activated += (s, e) =>
			{
				if (e.WindowActivationState != WindowActivationState.Deactivated && s is MainWindow win)
				{
					Windows.Remove(win);
					Windows.AddFirst(win);
					Focused?.Invoke(this);
				}
			};
			newWindow.Closed += (w, _) =>
			{
				if (w is MainWindow baw)
					Windows.Remove(baw);
				else
					throw new InvalidOperationException(
						$"Cannot remove browser application window from instance '{Name}', sender type mismatch.");
			};

			newWindow.SearchEngineChangeRequested += (se) => { Cache.CurrentSearchEngine = se; };

			newWindow.CurrentTheme = CurrentTheme;

			urls?.ToList().ForEach(url => newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url)));

			newWindow.Activate();

			return newWindow;
		}
		catch (Exception ex) //TODO: does nothing I think
		{
			ErrorInfo.AddError(ex); 
			
			return null;
		}
	}
	
}