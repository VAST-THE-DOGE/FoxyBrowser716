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
	public static ServerManager Instance { get; private set; }

	private ServerManager()
	{ /*TODO*/ }

	public static void RunServer(StartupEventArgs e)
	{
		Console.WriteLine("Starting server...");
		
		Instance = new ServerManager();
		Task.Run(() => Instance.StartPipeServer());
		if (e.Args.All(string.IsNullOrWhiteSpace))
		{
			Application.Current.Dispatcher.Invoke(() => 
			{
				var firstWindow = new MainWindow();
				firstWindow.Show();
			});
		}
		else
		{
			var url = e.Args.First(s => !string.IsNullOrWhiteSpace(s));
			Application.Current.Dispatcher.Invoke(async () => 
			{ 
				var newWindow = new MainWindow(); 
				await newWindow._initTask; 
				newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url)); 
				newWindow.Show(); 
			});
		}

		//TryInstall();
	}
	
	private void StartPipeServer()
	{
		while (true)
		{
			using var server = new NamedPipeServerStream("FoxyBrowser716_Pipe");
			server.WaitForConnection();
			using var reader = new System.IO.StreamReader(server);
			var message = reader.ReadLine();
			if (message.StartsWith("NewWindow|"))
			{
				var url = message.Replace("NewWindow|", "");
				Console.WriteLine("new window requested");
				Application.Current.Dispatcher.Invoke(async () => { 
					var newWindow = new MainWindow();
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

	private static void TryInstall()
	{
        var keyPath = @"SOFTWARE\Clients\StartMenuInternet\FoxyBrowser716";
        var baseKey = Registry.LocalMachine;

        // Try to open the key for reading
        using var regKey = baseKey.OpenSubKey(keyPath, writable: true);
        if (regKey == null)
        {

        }
	}
}