using System.Diagnostics;
using System.Threading;
using Windows.ApplicationModel;
using FoxyBrowser716.Controls.MainWindow;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using FoxyBrowser716.ErrorHandeler;
using Microsoft.UI.Dispatching;

namespace FoxyBrowser716.DataManagement;

public static class AppServer
{
	// private static Timer _backupTimer = new Timer();

	public static DispatcherQueue UiDispatcherQueue = null!;

	public static FoxyAutoSaver AutoSaver = null!;
	
	public static Instance CurrentInstance = null!;
	public static Instance PrimaryInstance = null!;
	
	public static VersionInfo VersionInfo => _versionInfo.Item!;
	private static readonly FoxyAutoSaverField<VersionInfo> _versionInfo =
		new(() => new VersionInfo { Version = null }, "VersionInfo.json", FoxyFileManager.FolderType.Cache);
	
	public static MainWindow? CurrentWindow => CurrentInstance?.CurrentWindow;
	
	public static readonly List<Instance> Instances = [];

	private static Timer _backupTimer;
	
	public static async Task HandleLaunchEvent(string[] uris, bool setupNeeded, bool fromStartup = false)
	{
		try
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
			
				UiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
				
				List<Task> tasks =
				[
					FoxyAutoSaver.Create([_versionInfo]).ContinueWith(task => AutoSaver = task.Result)
				];
				
				//TODO check for recovery file
			
				//TODO check for existing files and do any first time setup
			
				var instanceFolderPath = FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.Instance);
				var result = await FoxyFileManager.GetChildrenOfFolderAsync(instanceFolderPath, FoxyFileManager.ItemType.Folder);
				Task<Instance[]>? instanceTasks = null;
				if (result is { code: FoxyFileManager.ReturnCode.Success, items: not null })
				{
					instanceTasks = Task.WhenAll(
						result.items.Select(item => Instance.Create(item.path.Split(@"\")[^1]))
					);
					tasks.Add(instanceTasks);
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
				
				if (instanceTasks is not null)
					Instances.AddRange(await instanceTasks);

				//TODO: need a proper way to identify the primary instance
				PrimaryInstance = Instances.FirstOrDefault(i => i.Name == "Default")
				                  ?? Instances.FirstOrDefault()
				                  ?? throw new Exception("No instances found");

				CurrentInstance = PrimaryInstance;
			
				var backupRestored = false;
				if (BackupManagement.CheckForRestore())
				{
					await BackupManagement.RestoreBackup();
					backupRestored = true;
				}
				
				_backupTimer = new Timer(async _ => await BackupManagement.BackupData(), null, 10000, 5000);
				
				if (VersionInfo?.Version is null)
				{
					//TODO: 100% new
					// show a "thanks for downloading"
					
					VersionInfo!.Version = InfoGetter.VersionString;
				}
				else if (VersionInfo.Version != InfoGetter.VersionString)
				{
					//TODO: new update, go to changelog
					//TODO verify that this is not a dev version
					VersionInfo.Version = InfoGetter.VersionString;

					uris = [..uris, $"{InfoGetter.WebsiteUrl}/changelog/{VersionInfo.Version}"];
				}

				if (uris.Length > 0)
					await CurrentInstance.CreateWindow(uris);
				else if (!fromStartup && !backupRestored)
					await CurrentInstance.CreateWindow();
			}
			else if (!fromStartup)
			{
				UiDispatcherQueue.TryEnqueue(async () =>
				{
					try
					{
						if (uris.Length > 0)
							if (CurrentInstance.CurrentWindow is { } window)
								foreach (var uri in uris)
									window.TabManager.SwapActiveTabTo(window.TabManager.AddTab(uri));
							else
								await CurrentInstance.CreateWindow(uris);
						else
							await CurrentInstance.CreateWindow();
					}
					catch (Exception e)
					{
						Debug.WriteLine(e);
						ErrorInfo.AddError(e);
					}
				});
			}
		}
		catch (Exception e)
		{
			Debug.WriteLine(e);
			ErrorInfo.AddError(e);
		}
	}
}