using System.Timers;
using Windows.ApplicationModel;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Microsoft.UI.Dispatching;

namespace FoxyBrowser716_WinUI.DataManagement;

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
	
	public static async Task HandleLaunchEvent(string[] uris, bool setupNeeded)
	{
		//TODO
		/*var startupTask = await StartupTask.GetAsync("FoxyBrowserStartup");
    
		switch (startupTask.State)
		{
			case StartupTaskState.Disabled:
				var newState = await startupTask.RequestEnableAsync();
				break;
			case StartupTaskState.DisabledByUser:
				// User has disabled it in settings
				break;
			case StartupTaskState.Enabled:
				// Already enabled
				break;
		}*/

		
		if (setupNeeded)
		{
			await FoxyAutoSaver.Create([_versionInfo]).ContinueWith(task => AutoSaver = task.Result);
			
			List<Task> tasks = [];
			
			UiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
				
			//TODO check for recovery file
			
			//TODO check for existing files and do any first time setup
			
			var instanceFolderPath = FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Instance);
			var result = FoxyFileManager.GetChildrenOfFolder(instanceFolderPath, FoxyFileManager.ItemType.Folder);
			if (result.code == FoxyFileManager.ReturnCode.Success && result.items is not null)
			{
				tasks.AddRange(result.items.Select(async item => Instances.Add(await Instance.Create(item.path.Split(@"\")[^1]))));
				
				//TODO: need a proper way to identify the primary instance
			}
			else if (result.code == FoxyFileManager.ReturnCode.NotFound)
			{
				//TODO: first time setup
				throw new NotImplementedException();
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
			}

			if (uris.Length > 0)
				await Task.WhenAll(uris.Select(uri => CurrentInstance.CreateWindow(uri)));
			else
				await CurrentInstance.CreateWindow();
		}
		else
		{
			UiDispatcherQueue.TryEnqueue(async () =>
			{
				if (uris.Length > 0)
					await Task.WhenAll(uris.Select(uri => CurrentInstance.CreateWindow(uri)));
				else
					await CurrentInstance.CreateWindow();
			});
		}
	}
}