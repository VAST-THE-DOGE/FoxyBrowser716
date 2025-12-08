namespace FoxyBrowser716.DataManagement;

public static class BackupManagement
{
	public static bool CheckForRestore()
	{
		throw new NotImplementedException();
	}

	public static async Task RestoreBackup()
	{
		throw new NotImplementedException();
	}

	public static async Task BackupData()
	{
		List<WindowBackupModel> windows = [];
		
		foreach (var instance in AppServer.Instances)
		{
			foreach (var window in instance.Windows)
			{
				windows.Add(new WindowBackupModel
				{
					InstanceName = instance.Name,
					
					State = window.StateFromWindow(),
					Bounds = window.Bounds,
					
					Tabs = window.TabManager.Tabs.Select(t => t.Info.Url).ToArray(),
					TabGroups = window.TabManager.Groups.Select(g => new TabGroupBackupModel
					{
						Name = g.Name,
						GroupColor = g.GroupColor,
						Tabs = g.Tabs.Select(t => t.Info.Url).ToArray()
					}).ToArray()
				});
			}
		}

		var backup = new AppBackupModel()
		{
			Windows = windows.ToArray(),
		};
		
		throw new NotImplementedException();
	}
}