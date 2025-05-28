using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace FoxyBrowser716;

// layout plan:
// Server Manager (holds everything)
// breaks down into instances and windows



/// <summary>
/// Browser works as a server-client system to easily manage and sync data across windows
/// </summary>
public class ServerManager
{
	public static ServerManager Context { get; private set; }
	
	public InstanceManager DefaultBrowserManager { get; private set; }
	
	public List<InstanceManager> AllBrowserManagers = [];
	
	public List<MainWindow> BrowserWindows = [];
	
	private ServerManager()
	{ /*TODO*/ }
	
	public static void RunServer(StartupEventArgs e)
	{
		// list of tasks to do
		List<Task> tasks = [];
		
		// start a server instance
		Context = new ServerManager();
		Task.Run(() => Context.StartPipeServer());

		if (!Directory.Exists(InfoGetter.InstanceFolder))
		{
			Directory.CreateDirectory(InfoGetter.InstanceFolder);
			Directory.CreateDirectory(Path.Combine(InfoGetter.InstanceFolder, "Default"));
		}
		
		foreach (var path in Directory.GetDirectories(InfoGetter.InstanceFolder))
		{
			var instanceName = path.Split(@"\")[^1];
			if (instanceName == "Default") continue;
			
			Context.AllBrowserManagers.Add(new InstanceManager(instanceName));
		}
		
		// initialize that new server instance
		Context.DefaultBrowserManager = new InstanceManager("Default");
		Context.AllBrowserManagers.Add(Context.DefaultBrowserManager);
		tasks.Add(Context.DefaultBrowserManager.Initialize());
			
		// start the first browser window of the instance
		if (e.Args.All(string.IsNullOrWhiteSpace))
		{
			Application.Current.Dispatcher.Invoke(() => 
			{
				var firstWindow = new MainWindow(Context.DefaultBrowserManager);
				Context.BrowserWindows.Add(firstWindow);
				firstWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
				firstWindow.Show();
			});
		}
		else
		{
			var url = e.Args.First(s => !string.IsNullOrWhiteSpace(s));
			Application.Current.Dispatcher.Invoke(async () => 
			{ 
				var newWindow = new MainWindow(Context.DefaultBrowserManager); 
				await newWindow.InitTask; 
				newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url)); 
				Context.BrowserWindows.Add(newWindow);
				newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
				newWindow.Show(); 
			});
		}
	}
	
	private void StartPipeServer()
	{
		while (true)
		{
#if DEBUG
			using var server = new NamedPipeServerStream("DEBUG_FoxyBrowser716_Pipe");
#else
			using var server = new NamedPipeServerStream("FoxyBrowser716_Pipe");
#endif
			server.WaitForConnection();
			using var reader = new StreamReader(server);
			var message = reader.ReadLine();
			if (message?.StartsWith("NewWindow|")??false)
			{
				var url = message.Replace("NewWindow|", "");
				Application.Current.Dispatcher.Invoke(async () => { 
					var newWindow = new MainWindow(Context.DefaultBrowserManager);
					if (!string.IsNullOrWhiteSpace(url))
					{
						await newWindow.InitTask;
						newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
					}
					Context.BrowserWindows.Add(newWindow);
					newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
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
	
	public async Task CreateWindowFromTab(WebsiteTab tab, Rect finalRect, bool fullscreen, InstanceManager? instance = null)
	{
		var newWindow = new MainWindow(instance ?? Context.DefaultBrowserManager)
		{
			Top = finalRect.Y,
			Left = finalRect.X,
		};
		if (finalRect is { Height: > 50, Width: > 280 })
		{
			newWindow.Height = finalRect.Height;
			newWindow.Width = finalRect.Width;
		}
		
		await newWindow.InitTask; 
		newWindow.TabManager.SwapActiveTabTo(await newWindow.TabManager.TransferTab(tab)); 
		Context.BrowserWindows.Add(newWindow);
		newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
		newWindow.Show();

		if (fullscreen)
		{
			newWindow.WindowState = WindowState.Maximized;
		}
	}
	
	public async Task CreateWindow(Rect? finalRect = null, bool? fullscreen = null, InstanceManager? instance = null)
	{
		var newWindow = new MainWindow(instance ?? Context.DefaultBrowserManager);
		if (finalRect is { } fR)
		{
			newWindow.Top = fR.Y;
			newWindow.Left = fR.X;
		}
		
		if (finalRect is { Height: > 50, Width: > 280 } fR2)
		{
			newWindow.Height = fR2.Height;
			newWindow.Width = fR2.Width;
		}
		
		await newWindow.InitTask; 
		Context.BrowserWindows.Add(newWindow);
		newWindow.Closed += (w, _) => { Context.BrowserWindows.Remove((MainWindow)w); };
		newWindow.Show();

		if (fullscreen is true)
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