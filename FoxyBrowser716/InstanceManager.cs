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
	public string InstanceName {get; private set; }

	public readonly bool PrimaryInstance;
	
	public InstanceManager(string name)
	{
		InstanceName = name;
		PrimaryInstance = name == "Default";
		InstanceFolder = Path.Combine(ServerManager.InstanceFolderPath, InstanceName);
		if (!Directory.Exists(InstanceFolder)) Directory.CreateDirectory(InstanceFolder);
	}
		
	public async Task Initialize()
	{
		await LoadData();
	}

	public async Task SaveData()
	{
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