using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
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
				Instance.BrowserWindows.Add(firstWindow);
				firstWindow.Closed += (w, _) => { Instance.BrowserWindows.Remove((MainWindow)w); };
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
				Instance.BrowserWindows.Add(newWindow);
				newWindow.Closed += (w, _) => { Instance.BrowserWindows.Remove((MainWindow)w); };
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
					Instance.BrowserWindows.Add(newWindow);
					newWindow.Closed += (w, _) => { Instance.BrowserWindows.Remove((MainWindow)w); };
					newWindow.Show(); 
				});
			}
		}
	}

	public async Task<bool> TryTabTransfer(WebsiteTab tab, double left, double top)
	{
		var pos = new Point(left, top);

		foreach (var window in
		         (from window in BrowserWindows
			         let da = window.GetLeftBarDropArea()
			         let leftBoundS = da.X + 25
			         let topBoundS = da.Y + 25
			         let rightBoundS = da.X + da.Width + 50
			         let bottomBoundS = da.Y + da.Height + 50
			         where pos.X >= leftBoundS && pos.X <= rightBoundS && pos.Y >= topBoundS && pos.Y <= bottomBoundS
			         select window))
		{
			//TODO: modify this to keep the tab dragging
			await window.TabManager.TransferTab(tab);
			return true;
		}
		
		return false;
	}
	
	public async Task CreateWindowFromTab(WebsiteTab tab, Rect finalRect, bool fullscreen)
	{
		var newWindow = new MainWindow(Instance)
		{
			Top = finalRect.Y,
			Left = finalRect.X,
		};
		if (finalRect is { Height: > 50, Width: > 280 })
		{
			newWindow.Height = finalRect.Height;
			newWindow.Width = finalRect.Width;
		}
		
		await newWindow._initTask; 
		newWindow.TabManager.SwapActiveTabTo(await newWindow.TabManager.TransferTab(tab)); 
		Instance.BrowserWindows.Add(newWindow);
		newWindow.Closed += (w, _) => { Instance.BrowserWindows.Remove((MainWindow)w); };
		newWindow.Show();

		if (fullscreen)
		{
			newWindow.WindowState = WindowState.Maximized;
		}
	}

	
	private MainWindow? _lastOpenedWindow;

	public bool DoPositionUpdate(double left, double top)
	{
	    var pos = new Point(left, top);
	    var inExactDropArea = false;

	    foreach (var window in BrowserWindows)
	    {
	        var da = window.GetLeftBarDropArea();
	        
	        var leftBoundS = da.X;
	        var topBoundS = da.Y;
	        var rightBoundS = da.X + da.Width;
	        var bottomBoundS = da.Y + da.Height;
	        
	        var leftBoundB = da.X - 50;
	        var topBoundB = da.Y - 50;
	        var rightBoundB = da.X + da.Width + 100;
	        var bottomBoundB = da.Y + da.Height + 100;
	        
	        if (pos.X >= leftBoundS && pos.X <= rightBoundS && pos.Y >= topBoundS && pos.Y <= bottomBoundS)
	        {
	            inExactDropArea = true;
	        }
	        
	        if (pos.X >= leftBoundB && pos.X <= rightBoundB && pos.Y >= topBoundB && pos.Y <= bottomBoundB)
	        {
	            if (!window.SideOpen)
	            {
	                window.OpenSideBar();
	                _lastOpenedWindow = window;
	            }
	        }
	        else
	        {
	            if (window.SideOpen)
	            {
	                window.CloseSideBar();
	                if (_lastOpenedWindow == window)
	                {
	                    _lastOpenedWindow = null;
	                }
	            }
	        }
	    }

	    if (_lastOpenedWindow != null)
	    {
	        var lastDa = _lastOpenedWindow.GetLeftBarDropArea();
	        var lastLeftBoundB = lastDa.X - 50;
	        var lastTopBoundB = lastDa.Y - 50;
	        var lastRightBoundB = lastDa.X + lastDa.Width + 100;
	        var lastBottomBoundB = lastDa.Y + lastDa.Height + 100;
	        
	        if (!(pos.X >= lastLeftBoundB && pos.X <= lastRightBoundB && pos.Y >= lastTopBoundB && pos.Y <= lastBottomBoundB))
	        {
	            _lastOpenedWindow.CloseSideBar();
	            _lastOpenedWindow = null;
	        }
	    }

	    return inExactDropArea;
	}
}