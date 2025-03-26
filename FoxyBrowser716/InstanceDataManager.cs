using System.IO;

namespace FoxyBrowser716;

public class InstanceDataManager
{
	public readonly WebsiteInfoList PinInfo = new();
	public readonly WebsiteInfoList BookmarkInfo = new();

	private readonly ServerManager _instance;

	public InstanceDataManager(ServerManager instance)
	{
		_instance = instance;
	}
		
	public async Task Initialize()
	{
		var instanceFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _instance.InstanceName);

		//TODO: check if folder exist and create the instance folder if not.
		await Task.WhenAll(
			PinInfo.LoadTabInfoFromJson(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pins.json")),
			BookmarkInfo.LoadTabInfoFromJson(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bookmarks.json"))
		);
		
		
	}
}