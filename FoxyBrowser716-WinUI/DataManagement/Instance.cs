using Windows.Foundation;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataObjects;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using FoxyBrowser716_WinUI.StaticData;
using WinUIEx;

namespace FoxyBrowser716_WinUI.DataManagement;

public class Instance
{
	public string Name { get; private set; }

	
	public InstanceSettings Settings => _settings.Item;
	public InstanceCache Cache => _cache.Item;
	private FoxyAutoSaverField<InstanceSettings> _settings;
	private FoxyAutoSaverField<InstanceCache> _cache;
	
	public ObservableCollection<WebsiteInfo> Pins => _pins.Items;
	public ObservableCollection<WebsiteInfo> Bookmarks => _bookmarks.Items;
	private FoxyAutoSaverList<WebsiteInfo> _pins;
	private FoxyAutoSaverList<WebsiteInfo> _bookmarks;
	
	public bool IsPrimaryInstance => Name == AppServer.PrimaryInstance.Name;

	public LinkedList<MainWindow> Windows = [];
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
	
	public event Action<Instance>? Focused;
	
	private Instance()
	{}

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
		_settings = new(() => new InstanceSettings(), "Settings.json", FoxyFileManager.FolderType.Data, Name);
		
		_pins = new("Pins.json", FoxyFileManager.FolderType.Data, Name);
		_bookmarks = new("Bookmarks.json", FoxyFileManager.FolderType.Data, Name);
			
		await AppServer.AutoSaver.AddItems([
			_settings,
			_cache,
			_pins,
			_bookmarks
		]);
	}
	
	public async Task<MainWindow> CreateWindow(string? url = null, Rect? startLocation = null, MainWindow.BrowserWindowState windowState = MainWindow.BrowserWindowState.Normal)
	{
		var newWindow = await MainWindow.Create(this);
		
		Focused?.Invoke(this);
		
		if (startLocation is { } sl)
		{
			newWindow.MoveAndResize(sl.X, sl.Y, sl.Width, sl.Height);;
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

		newWindow.SearchEngineChangeRequested += (se) =>
		{
			Cache.CurrentSearchEngine = se;
		};

		if (url != null)
		{
			newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
		}
		
		newWindow.Activate();
		return newWindow;
	}
	
}