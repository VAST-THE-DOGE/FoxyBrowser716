using System.Timers;
using Windows.ApplicationModel;
using FoxyBrowser716.Controls.MainWindow;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Microsoft.UI.Dispatching;

namespace FoxyBrowser716.DataManagement;

public static class AppServer
{
	private static Timer _backupTimer = new Timer();

	public static DispatcherQueue UiDispatcherQueue;

	public static FoxyAutoSaver AutoSaver;
	
	public static Instance CurrentInstance;
	public static Instance PrimaryInstance;
	
	private static FoxyAutoSaverField<VersionInfo> _versionInfo = new(() => new VersionInfo { Version = null }, "VersionInfo.json", FoxyFileManager.FolderType.Cache);
	
	public static MainWindow? CurrentWindow => CurrentInstance?.CurrentWindow;
	
	public static List<Instance> Instances = [];
	
	public static async Task HandleLaunchEvent(string[] uris, bool setupNeeded, bool fromStartup = false)
	{
		if (setupNeeded)
		{
			var startupTask = await StartupTask.GetAsync("FoxyBrowserStartup");
		
			switch (startupTask.State)
			{
				case StartupTaskState.Disabled:
					var newState = await startupTask.RequestEnableAsync();
					break;
				case StartupTaskState.DisabledByUser:
					//TODO: popup to re-enable
					break;
			}
			
			await FoxyAutoSaver.Create([_versionInfo]).ContinueWith(task => AutoSaver = task.Result);
			
			List<Task> tasks = [];
			
			UiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
				
			//TODO check for recovery file
			
			//TODO check for existing files and do any first time setup
			
			var instanceFolderPath = FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Instance);
			var result = await FoxyFileManager.GetChildrenOfFolderAsync(instanceFolderPath, FoxyFileManager.ItemType.Folder);
			if (result.code == FoxyFileManager.ReturnCode.Success && result.items is not null)
			{
				var instances = await Task.WhenAll(
					result.items.Select(item => Instance.Create(item.path.Split(@"\")[^1]))
				);
				Instances.AddRange(instances);
				
				//TODO: need a proper way to identify the primary instance
			}
			else if (result.code == FoxyFileManager.ReturnCode.NotFound)
			{
				// TODO this is running on laptop:
				
				//TODO: first time setup
				//throw new NotImplementedException();
				
				Instances.Add(await Instance.Create("Default"));
			}
			else
				throw new Exception($"Failed to get instances in {instanceFolderPath}: {result.code}");
			
			await Task.WhenAll(tasks);
			
			
			PrimaryInstance = Instances.FirstOrDefault(i => i.Name == "Default")
			                  ?? Instances.FirstOrDefault()
			                  ?? throw new Exception("No instances found");

			CurrentInstance = PrimaryInstance;
			
			if (_versionInfo.Item?.Version is null)
			{
				//TODO: 100% new
				
				_versionInfo.Item.Version = InfoGetter.VersionString;
			}
			else if (_versionInfo.Item.Version != InfoGetter.VersionString)
			{
				//TODO: new update, go to changelog
				_versionInfo.Item.Version = InfoGetter.VersionString;
				uris = uris
					.ToList()
					.Append($"{InfoGetter.WebsiteUrl}/changelog/{_versionInfo.Item.Version}")
					.ToArray();
			}

			if (uris.Length > 0)
				await CurrentInstance.CreateWindow(uris);
			else if (!fromStartup)
				await CurrentInstance.CreateWindow();
		}
		else
		{
			UiDispatcherQueue.TryEnqueue(async () =>
			{
				if (uris.Length > 0)
					if (CurrentInstance.CurrentWindow is { } window)
						foreach (var uri in uris)
							window.TabManager.SwapActiveTabTo(window.TabManager.AddTab(uri)); //TODO: NEED TO BE ASYNC
					else
						await CurrentInstance.CreateWindow(uris);
				else
					await CurrentInstance.CreateWindow();
			});
		}
	}
}