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
	
	private System.Timers.Timer _backupTimer;
	
	public InstanceManager DefaultBrowserManager { get; private set; }
	
	public List<InstanceManager> AllBrowserManagers = [];
	
	public InstanceManager CurrentBrowserManager;

	public IReadOnlyList<BrowserApplicationWindow> AllBrowserWindows 
		=> AllBrowserManagers.SelectMany(m => m.BrowserWindows).ToList();

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

			var newInstance = new InstanceManager(instanceName);
			
			Context.AllBrowserManagers.Add(newInstance);
			
			newInstance.Focused += manager =>
			{
				Context.CurrentBrowserManager = manager;
			};
		}
		
		// initialize that new server instance
		Context.DefaultBrowserManager = new InstanceManager("Default");
		Context.DefaultBrowserManager.Focused += manager =>
		{
			Context.CurrentBrowserManager = manager;
		};
		Context.AllBrowserManagers.Add(Context.DefaultBrowserManager);
		Context.CurrentBrowserManager = Context.DefaultBrowserManager;
		tasks.Add(Context.DefaultBrowserManager.Initialize());
		
		//check if the application was not closed correctly:
		var checkFile = Path.Combine(InfoGetter.AppData, "ERROR_CHECK.txt");
		if (File.Exists(checkFile) && File.Exists(BackupManager.BackupFilePath))
		{
			var popup = new FoxyPopup()
			{
				Title = "Backup Found",
				Subtitle = "FoxyBrowser was closed unexpectedly.\nDo you wish to load a backup file to restore the previous application state?",
				ShowProgressbar = false,
			};
			popup.SetButtons(new FoxyPopup.BottomButton(() => {
				Application.Current.Dispatcher.Invoke(async () =>
				{
					popup.ShowProgressbar = true;
					popup.Subtitle = "Restoring backup...";
					await BackupManager.RestoreFromBackup(Context);
					AfterPopupSetup(e, checkFile, true);
					popup.Close();
				});
			}, "Yes"), new FoxyPopup.BottomButton(() =>
			{
				AfterPopupSetup(e, checkFile, false);
				popup.Close();
			}, "No"));
			 
			popup.Show();
		}
		else
		{
			AfterPopupSetup(e, checkFile, false);
		}
	}

	private static void AfterPopupSetup(StartupEventArgs e, string checkFile, bool backupRestored)
	{
		// create the check file
		File.WriteAllText(checkFile, "Do not delete this file.\nThis is used to check if the application was closed correctly and restore any tabs when you reopen it.");
		
		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
		{
			// normal application exit, remove the check
			File.Delete(checkFile);
		};
		
		Context._backupTimer = new System.Timers.Timer(5000);
		Context._backupTimer.Elapsed += async (_,_) => await Context.DoBackup();
		Context._backupTimer.AutoReset = true;
		Context._backupTimer.Enabled = true;
		
		// if the backup is restored, do not do anything:
		if (backupRestored) return;
		
		// start the first browser window of the instance
		if (e.Args.All(string.IsNullOrWhiteSpace))
		{
			Application.Current.Dispatcher.Invoke(async () =>
			{
				await Context.DefaultBrowserManager.CreateWindow();
			});
		}
		else
		{
			var url = e.Args.First(s => !string.IsNullOrWhiteSpace(s));
			Application.Current.Dispatcher.Invoke(async () => 
			{
				if (Context.CurrentBrowserManager is { } cbm)
				{
					if (cbm.CurrentBrowserWindow is { } cbw)
					{
						cbw.TabManager.SwapActiveTabTo(cbw.TabManager.AddTab(url));
					}
					else
					{
						await cbm.CreateWindow(url);
					}
				}
				else
				{
					if (Context.DefaultBrowserManager.CurrentBrowserWindow is { } cbw)
					{
						cbw.TabManager.SwapActiveTabTo(cbw.TabManager.AddTab(url));
					}
					else
					{
						await Context.DefaultBrowserManager.CreateWindow(url);
					}
				}
			});
		}
	}

	private async Task DoBackup()
	{
		await Application.Current.Dispatcher.Invoke(async () => await BackupManager.RefreshBackupFile(Context));
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
					
					if (!string.IsNullOrWhiteSpace(url))
					{
						if (CurrentBrowserManager.CurrentBrowserWindow is { } cbw)
							cbw.TabManager.SwapActiveTabTo(cbw.TabManager.AddTab(url));
						else
						{
							var newWindow = await CurrentBrowserManager.CreateWindow(url);
							newWindow.TabManager.SwapActiveTabTo(newWindow.TabManager.AddTab(url));
						}
					}
					else
					{
						await CurrentBrowserManager.CreateWindow();
					}
				});
			}
		}
	}

	public async Task<bool> TryTabTransfer(WebsiteTab tab, Point mid)
	{

		foreach (var window in
		         (from window in AllBrowserWindows
			         let da = window.GetLeftBarDropArea()
			         let leftBoundS = da.X - 5
			         let topBoundS = da.Y - 5
			         let rightBoundS = da.X + da.Width + 5
			         let bottomBoundS = da.Y + da.Height + 5
			         where mid.X >= leftBoundS && mid.X <= rightBoundS && mid.Y >= topBoundS && mid.Y <= bottomBoundS
			         select window))
		{
			//TODO: modify this to keep the tab dragging
			await window.TabManager.TransferTab(tab);
			return true;
		}
		
		return false;
	}

	private BrowserApplicationWindow? _lastOpenedWindow
		=> CurrentBrowserManager.CurrentBrowserWindow;

	public bool DoPositionUpdate(Point mid)
	{
	    var inExactDropArea = false;

	    foreach (var window in AllBrowserWindows)
	    {
	        var da = window.GetLeftBarDropArea();
	        
	        var leftBoundS = da.X;
	        var topBoundS = da.Y;
	        var rightBoundS = da.X + da.Width;
	        var bottomBoundS = da.Y + da.Height;
	        
	        var leftBoundB = da.X - 150;
	        var topBoundB = da.Y - 150;
	        var rightBoundB = da.X + da.Width + 150;
	        var bottomBoundB = da.Y + da.Height + 150;
	        
	        if (mid.X >= leftBoundS && mid.X <= rightBoundS && mid.Y >= topBoundS && mid.Y <= bottomBoundS)
	        {
	            inExactDropArea = true;
	        }
	        
	        if (mid.X >= leftBoundB && mid.X <= rightBoundB && mid.Y >= topBoundB && mid.Y <= bottomBoundB)
	        {
	            if (!window.SideOpen)
	            {
	                window.OpenSideBar();
	                // _lastOpenedWindow = window;
	            }
	        }
	        else
	        {
	            if (window.SideOpen)
	            {
	                window.CloseSideBar();
	                if (_lastOpenedWindow == window)
	                {
	                    // _lastOpenedWindow = null;
	                }
	            }
	        }
	    }

	    if (_lastOpenedWindow != null)
	    {
	        var lastDa = _lastOpenedWindow.GetLeftBarDropArea();
	        var lastLeftBoundB = lastDa.X - 100;
	        var lastTopBoundB = lastDa.Y - 100;
	        var lastRightBoundB = lastDa.X + lastDa.Width + 100;
	        var lastBottomBoundB = lastDa.Y + lastDa.Height + 100;
	        
	        if (!(mid.X >= lastLeftBoundB && mid.X <= lastRightBoundB && mid.Y >= lastTopBoundB && mid.Y <= lastBottomBoundB))
	        {
	            _lastOpenedWindow.CloseSideBar();
	            //_lastOpenedWindow = null;
	        }
	    }

	    return inExactDropArea;
	}
}