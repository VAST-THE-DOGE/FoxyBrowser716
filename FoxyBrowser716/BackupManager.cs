using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;

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
}