using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.ErrorHandeler;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716.DataObjects.Complex;

public class WebviewTab
{
	private static int _tabCounter;
	
	public WebsiteInfo Info { get; init; }
	public TabManager TabManager;
	private string? _startingUrl;
	public int Id { get; private set; }
	public WebView2 Core { get; private set; }
	
	public Task InitializeTask { get; private set; }
	
	public WebviewTab(TabManager tabManager, string? url)
	{
		var core = new WebView2();
		var info = new WebsiteInfo()
		{
			Url = url ?? "",
			Title = "Loading...",
			FavIconUrl = "", //TODO: get an icon on the website setup to grab from
		};
		
		TabManager = tabManager;
		_startingUrl = url;
				
		Id = Interlocked.Increment(ref _tabCounter);
		Info = info;
		Core = core;
		
		InitializeTask = CoreWebView2Initialization();
	}

	private async Task CoreWebView2Initialization()
	{
		await Core.EnsureCoreWebView2Async(TabManager.WebsiteEnvironment);

		// await TabManager.Instance.AddExtensions(Core);
		//  TabManager.Instance.RegisterStoreButtonCallback(json => {
		// 	// json contains: host, url, originalText, type
		// 	// e.g. read the url:
		// 	if (json.TryGetProperty("url", out var urlProp)) {
		// 		var url = urlProp.GetString();
		// 		// do something: log, open UI, start extension install, etc.
		// 		System.Diagnostics.Debug.WriteLine("Clicked store button on: " + url);
		// 	}
		// });
		
		//await TabManager.Instance.InjectStoreButtonInterceptor(Core);

		var extensionSetupTask = TabManager.Instance.SetupExtensionSupport(Core);
		
		Core.AllowDrop = true;
		Core.CoreWebView2.DefaultDownloadDialogCornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
		Core.CoreWebView2.DefaultDownloadDialogMargin = new Point(0, 0);
		Core.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
		Core.CoreWebView2.Settings.AreDevToolsEnabled = true;
		
		// Add after Core.CoreWebView2.Settings.AreDevToolsEnabled = true;
		// Core.CoreWebView2.Settings.IsWebMessageEnabled = false;     
		Core.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
		Core.CoreWebView2.Profile.IsPasswordAutosaveEnabled = false;

		Core.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;
		Core.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

		// handle events
		Core.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
		Core.CoreWebView2.FaviconChanged += OnFaviconChanged;
		Core.CoreWebView2.SourceChanged += CoreWebView2OnSourceChanged;
		Core.CoreWebView2.NewWindowRequested += CoreWebView2OnNewWindowRequested;
		Core.CoreWebView2.NavigationStarting += CoreWebView2OnNavigationStarting;
		Core.CoreWebView2.WindowCloseRequested += CoreWebView2OnWindowCloseRequested;
		Core.CoreWebView2.ProcessFailed += CoreWebView2OnProcessFailed;

		Core.CoreWebView2.PermissionRequested += CoreWebView2OnPermissionRequested;
		//performance stuff
		var processId = Core.CoreWebView2.BrowserProcessId;
		var process = Process.GetProcessById((int)processId);
        
		process.PriorityClass = ProcessPriorityClass.High;
		
		// process.ProcessorAffinity = 0x0F; // Use first 4 cores
		
		try
		{
			var gpuProcesses = Process.GetProcessesByName("msedgewebview2")
				.Where(p => p.Id != process.Id);
    
			foreach (var gpuProcess in gpuProcesses)
			{
				try
				{
					gpuProcess.PriorityClass = ProcessPriorityClass.High;
				}
				catch { /* Ignore */ }
			}
		}
		catch { /* Ignore */ }

		
		await Task.WhenAll(extensionSetupTask, NavigateOrSearch(_startingUrl, true));
	}

	private void CoreWebView2OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
	{
		//TODO: temp fix
		if (args.Uri.StartsWith("chrome-search://local-ntp/local-ntp.html"))
		{
			args.Cancel = true;
		}
	}

	private void CoreWebView2OnPermissionRequested(CoreWebView2 sender, CoreWebView2PermissionRequestedEventArgs args)
	{
		//temp to get rid of the camera request
		if (args.PermissionKind == CoreWebView2PermissionKind.Camera)
			args.Handled = true;
		
		//TODO handle this properly
	}

	private void CoreWebView2OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
	{
		ErrorInfo.AddWarning($"{nameof(CoreWebView2OnProcessFailed)} - {args.Reason} ({args.ExitCode})", 
			$"{nameof(args.FailureSourceModulePath)}: {args.FailureSourceModulePath}\n{nameof(args.ProcessFailedKind)}: {args.ProcessFailedKind}\n{nameof(args.ProcessDescription)}: {args.ProcessDescription}");
		
		//try a recovery
		TabManager.RemoveTab(Id);
		TabManager.SwapActiveTabTo(TabManager.AddTab(Core.Source.ToString()));
	}

	private void CoreWebView2OnWindowCloseRequested(CoreWebView2 sender, object args)
	{
		TabManager.RemoveTab(Id);
	}

	private void CoreWebView2OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
	{
		TabManager.SwapActiveTabTo(TabManager.AddTab(args.Uri));
		args.Handled = true;
	}

	private void CoreWebView2OnSourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
	{
		Info.Url = Core.CoreWebView2.Source;
	}

	public async Task NavigateOrSearch(string? url) => await NavigateOrSearch(url, false);
	
	private async Task NavigateOrSearch(string? url, bool forceNav)
	{
		if (url is null) return;
		
		if (!InitializeTask.IsCompleted && !forceNav)
		{
			_startingUrl = url;
			return;
		}

		try
		{
			Core.CoreWebView2.Navigate(url);
		}
		catch
		{
			if (url.Contains('.') || url.Contains(':') || url.Contains('/'))
				try
				{
					Core.CoreWebView2.Navigate("https://" + url);
				}
				catch
				{
					try
					{
						Core.CoreWebView2.Navigate("http://" + url);
					}
					catch
					{
						Core.CoreWebView2.Navigate(InfoGetter.GetSearchUrl(TabManager.Instance.Cache.CurrentSearchEngine, url));
					}
				}
			else
				Core.CoreWebView2.Navigate(InfoGetter.GetSearchUrl(TabManager.Instance.Cache.CurrentSearchEngine, url));
		}
	}

	private void OnDocumentTitleChanged(object sender, object e)
	{
		Info.Title = Core.CoreWebView2.DocumentTitle;
	}

	private async void OnFaviconChanged(object sender, object e)
	{
		Info.FavIconUrl = Core.CoreWebView2.FaviconUri ?? "";
	}
}