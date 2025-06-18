using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace FoxyBrowser716;

public static class BackupManager
{
	public static readonly string BackupFilePath = Path.Combine(InfoGetter.AppData, "Backup.json");
	private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions {
		WriteIndented = true,
	};
	
	private static bool _inBackupFunction;
	public static async Task RefreshBackupFile(ServerManager context)
	{
		try
		{
			if (_inBackupFunction) return;
			_inBackupFunction = true;
			
			var windowIdGetter = 0;
		
			// create the backup data structure
			var newBackup = new BackupFile
			{
				Instances = context.AllBrowserManagers
					.Select(manager => new SimpleInstance
					{
						//TODO: verify that this works
						CurrentWindowId = windowIdGetter, // due to the order by, this is always correct
						Windows = manager.BrowserWindows
							.OrderByDescending(window => window == manager.CurrentBrowserWindow)
							.Select(window => new SimpleWindow
							{
								OpenTabs = window.TabManager
									.GetAllTabs()
									.Where(pair => pair.Value?.TabCore?.Source != null)
									.ToDictionary(pair => pair.Key, pair => pair.Value.TabCore.Source.ToString()),
								ActiveTabId = window.TabManager.ActiveTabId,
							
								WindowId = windowIdGetter++,
								WindowRect = new Rect(window.Left, window.Top, window.Width, window.Height),
								WindowState = InstanceManager.StateFromWindow(window),
							
							})
							.ToList(),
						InstanceName = manager.InstanceName
					})
					.ToList(),
				CurrentInstanceName = context.CurrentBrowserManager?.InstanceName
			};
			// save the backup
			var json = JsonSerializer.Serialize(newBackup, _serializerOptions);
			await File.WriteAllTextAsync(BackupFilePath, json);
			
			_inBackupFunction = false;
		}
		catch (Exception e)
		{
			_inBackupFunction = false;
			throw;
		}
	}
	
	public static async Task RestoreFromBackup(ServerManager context)
	{
		// sanity check
		if (!File.Exists(BackupFilePath)) throw new FileNotFoundException("Backup file not found");
		
		// get the backup data
		var json = await File.ReadAllTextAsync(BackupFilePath);
		var backup = JsonSerializer.Deserialize<BackupFile>(json, _serializerOptions);
		
		if (backup == null) throw new Exception("Backup file is not a valid backup file");
		
		// restore the data
		InstanceManager? managerToFocus = null;
		var instanceTasks = backup.Instances.Select(async instance =>
		{
			if (context.AllBrowserManagers.FirstOrDefault(manager => manager.InstanceName == instance.InstanceName)
			    is { } instanceManager)
			{
				BrowserApplicationWindow? windowToFocus = null;
				var instanceTasks = instance.Windows.Select(async window =>
				{
					var newWindow = await instanceManager.CreateWindow(null, window.WindowRect, window.WindowState);
					int TabToFocus = window.ActiveTabId < 0 ? window.ActiveTabId : -1;
					foreach (var (tabId, url) in window.OpenTabs)
					{
						var id = newWindow.TabManager.AddTab(url);

						var tab = newWindow.TabManager.GetTab(id);
						if (tab is not null)
						{
							var init = AwaitCoreWebView2Initialization(tab.TabCore);
							newWindow.TabManager.SwapActiveTabTo(id);
							
							if (tabId == window.ActiveTabId)
							{
								TabToFocus = id;
							}
							
							// wait for the tab to initialize
							await init;
						}
					}

					
					newWindow.TabManager.SwapActiveTabTo(TabToFocus);
					
					if (window.WindowId == instance.CurrentWindowId)
					{
						windowToFocus = newWindow;
					}
				});
				
				windowToFocus?.Focus();

				if (instance.InstanceName == backup.CurrentInstanceName)
				{
						managerToFocus = instanceManager;
				}
				await Task.WhenAll(instanceTasks);
			}
			else
			{
				// TODO: handle better, but for now just skip as the instance may not exist anymore?
			}
		});
		await Task.WhenAll(instanceTasks);
		
		if (managerToFocus is not null)
			context.CurrentBrowserManager = managerToFocus;
	}
	
	private record BackupFile
	{
		public required List<SimpleInstance> Instances { get; set; } = [];
		public required string? CurrentInstanceName { get; set; } = null;
	}

	private record SimpleInstance
	{
		public required List<SimpleWindow> Windows { get; set; } = [];
		public required int? CurrentWindowId { get; set; } = null;
		public required string InstanceName { get; set; }
	}

	private record SimpleWindow
	{
		public required Dictionary<int, string> OpenTabs { get; set; } = [];
		public required int ActiveTabId { get; set; }
		public required int WindowId { get; set; }
		
		public required Rect WindowRect { get; set; }
		public required InstanceManager.BrowserWindowState WindowState { get; set; }
	}
	
	private static async Task AwaitCoreWebView2Initialization(WebView2CompositionControl tabCore)
	{
		var tcs = new TaskCompletionSource<object>();
		EventHandler<CoreWebView2InitializationCompletedEventArgs> handler = null;

		handler = (_, args) =>
		{
			tabCore.CoreWebView2InitializationCompleted -= handler;
			if (args.IsSuccess)
				tcs.TrySetResult(null);
			else
				tcs.TrySetException(args.InitializationException);
		};

		tabCore.CoreWebView2InitializationCompleted += handler;
		
		var timeoutTask = Task.Delay(5000);
		var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

		if (completedTask == timeoutTask)
		{
			tabCore.CoreWebView2InitializationCompleted -= handler; 
			return;
		}

		await tcs.Task;
	}
}