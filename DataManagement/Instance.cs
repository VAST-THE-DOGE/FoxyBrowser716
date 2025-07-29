using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataObjects;
using FoxyBrowser716_WinUI.StaticData;

namespace FoxyBrowser716_WinUI.DataManagement;

public class Instance
{
	public readonly string InstanceName;

	public InstanceSettings Settings;
	
	public bool IsPrimaryInstance => InstanceName == "TODO: AppServerField";

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
	
	public Instance(string name)
	{
		InstanceName = name;

		//TODO: find a good way to force initialize to be called before using the instance.
	}

	public async Task Initialize(InstanceRetoreData? retoreData = null)
	{
		var isNewInstance = false;
		
		//TODO: optimize this later on. Should be a Task.WhenAll ot load all at one time.
		
		var result = await FoxyFileManager.ReadFromFileAsync<InstanceSettings>(
			FoxyFileManager.BuildFilePath("Settings.json", FoxyFileManager.FolderType.Data, InstanceName));
		if (result.code == FoxyFileManager.ReturnCode.Success && result.content is { } settings)
		{
			Settings = settings;
		}
		else if (result.code == FoxyFileManager.ReturnCode.NotFound)
		{
			
		}
		else
		{
			throw new Exception($"Failed to load settings.json for instance {InstanceName}: {result.code}");
		}
	}
}