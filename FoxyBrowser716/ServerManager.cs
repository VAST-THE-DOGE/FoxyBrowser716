using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace FoxyBrowser716;

/// <summary>
/// Browser works as a server-client system to easily manage and sync data across windows
/// </summary>
public class ServerManager
{
	private static ServerManager Instance { get; set; }

	public InstanceDataManager BrowserData { get; private set; }
	
	public List<MainWindow> BrowserWindows = [];

	public readonly string InstanceName = "Main";
	
	private ServerManager()
	{ /*TODO*/ }

	public static void RunServer(StartupEventArgs e)
	{
		// list of tasks to do
		List<Task> tasks = [];
		
		// check if it is installed.
		if (!InstallationManager.IsBrowserInstalled()
		    && MessageBox.Show($"Would you like to register {InstallationManager.GetApplicationName()} as an app (allows you to set it as the default browser in windows settings)?", "Finish Install?", MessageBoxButton.YesNo) 
		    == MessageBoxResult.Yes)
		{
			InstallationManager.RegisterBrowser();
		}
		
		// start a server instance
		Instance = new ServerManager();
		Task.Run(() => Instance.StartPipeServer());
		
		// initialize that new server instance
		Instance.BrowserData = new InstanceDataManager(Instance);
		tasks.Add(Instance.BrowserData.Initialize());
			
		// start the first browser window of the instance
		if (e.Args.All(string.IsNullOrWhiteSpace))
		{
			Application.Current.Dispatcher.Invoke(() => 
			{
				var firstWindow = new MainWindow(Instance);
				firstWindow.Show();
			});
		}
		else
		{
			var url = e.Args.First(s => !string.IsNullOrWhiteSpace(s));
			Application.Current.Dispatcher.Invoke(async () => 
			{ 
				var newWindow = new MainWindow(Instance); 
				await newWindow._initTask; 
				newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url)); 
				newWindow.Show(); 
			});
		}
		
		
	}
	
	private void StartPipeServer()
	{
		while (true)
		{
			using var server = new NamedPipeServerStream("FoxyBrowser716_Pipe");
			server.WaitForConnection();
			using var reader = new StreamReader(server);
			var message = reader.ReadLine();
			if (message?.StartsWith("NewWindow|")??false)
			{
				var url = message.Replace("NewWindow|", "");
				Application.Current.Dispatcher.Invoke(async () => { 
					var newWindow = new MainWindow(Instance);
					if (!string.IsNullOrWhiteSpace(url))
					{
						await newWindow._initTask;
						newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
					}
					newWindow.Show(); 
				});
			}
		}
	}
}