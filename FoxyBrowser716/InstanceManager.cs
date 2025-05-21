using System.IO;
using FoxyBrowser716.Settings;

namespace FoxyBrowser716;

public class InstanceManager
{
	public readonly WebsiteInfoList PinInfo = new();
	public readonly WebsiteInfoList BookmarkInfo = new();
	public readonly BrowserSettingsManager SettingsManager = new();
	
	public List<MainWindow> BrowserWindows = [];

	public readonly string InstanceFolder;
	public readonly string ExtensionFolder;
	
	public string InstanceName {get; private set; }

	public readonly bool PrimaryInstance;
	
	public InstanceManager(string name)
	{
		InstanceName = name;
		PrimaryInstance = name == "Default";
		InstanceFolder = Path.Combine(InfoGetter.InstanceFolder, InstanceName);
		ExtensionFolder = Path.Combine(InstanceFolder, "extensions");

		if (!Directory.Exists(InstanceFolder)) Directory.CreateDirectory(InstanceFolder);
	}
		
	public async Task Initialize()
	{
		await LoadData();
	}

	public async Task SaveData()
	{
		// note, WebsiteInfoList handles saving on add and remove on its own
		//TODO
	}

	public async Task LoadData()
	{
		await Task.WhenAll(
			PinInfo.LoadTabInfoFromJson(Path.Combine(InstanceFolder, "pins.json")),
			BookmarkInfo.LoadTabInfoFromJson(Path.Combine(InstanceFolder, "bookmarks.json"))
		);
		//TODO
	}

	public async Task RenameInstance()
	{
		//TODO
	}
}