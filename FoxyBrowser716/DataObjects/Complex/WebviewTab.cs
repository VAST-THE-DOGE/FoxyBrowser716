using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using Windows.Foundation;
using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.ErrorHandeler;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716.DataObjects.Complex;

public partial class WebviewTab : ObservableObject
{
	private static int _tabCounter;
	
	[ObservableProperty] public partial bool IsActive { get; set; }
	[ObservableProperty] public partial WebsiteInfo Info { get; set; }
	public TabManager TabManager;
	private string? _startingUrl;
	public int Id { get; private set; }
	[JsonIgnore] [ObservableProperty] public partial bool MovingTab { get; set; }
	
	[ObservableProperty] public partial WebView2 Core { get; private set; }
	
	public Task InitializeTask { get; private set; }
	
	private static readonly HashSet<int> _boostedProcessIds = [];
	private static readonly object _boostLock = new();
	
	public WebviewTab(TabManager tabManager, string? url)
	{
		var core = new WebView2();
		var info = new WebsiteInfo
		{
			Url = url ?? "",
			Title = "Loading...",
			FavIconUrl = "",
		};
		
		TabManager = tabManager;
		_startingUrl = url;
				
		Id = Interlocked.Increment(ref _tabCounter);
		Info = info;
		Core = core;
		
		InitializeTask = CoreWebView2Initialization();
		
		GetMenuItems = () =>
		{
			var items = new ObservableCollection<FMenuItem>(BaseItems);

			if (!TabManager.Tabs.Contains(this))
			{
				items.Add(new() { Text = "Remove from group", Action = () => TabManager.MoveTabToGroup(Id, -1) });
			}

			foreach (var group in TabManager.Groups.Where(g => !g.Tabs.Contains(this)))
			{
				items.Add(new() { Text = $"Move to {group.Name}", Action = () => TabManager.MoveTabToGroup(Id, group.Id) });
			}

			return items;
		};
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
		
		// Core.AllowExternalDrop = true;
		Core.AllowDrop = true;
		// Core.CompositeMode = ElementCompositeMode.MinBlend;
		
		Core.CoreWebView2.DefaultDownloadDialogCornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
		Core.CoreWebView2.DefaultDownloadDialogMargin = new Point(0, 0);
		Core.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
		// Core.CoreWebView2.Settings.AreDevToolsEnabled = true;
		
		// Core.CoreWebView2.Settings.IsWebMessageEnabled = false;     
		// Core.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
		// Core.CoreWebView2.Profile.IsPasswordAutosaveEnabled = false;

		// Core.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;
		// Core.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

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
		
		_ = Task.Run(() =>
		{
			try
			{
				BoostProcessPriority((int)processId);
			}
			catch { /* Ignore */ }
		});

		await Task.WhenAll(extensionSetupTask, NavigateOrSearch(_startingUrl, true));
	}
	
	private static void BoostProcessPriority(int processId)
	{
		lock (_boostLock)
		{
			if (_boostedProcessIds.Contains(processId))
				return;

			try
			{
				var process = Process.GetProcessById(processId);
				process.PriorityClass = ProcessPriorityClass.AboveNormal;
				_boostedProcessIds.Add(processId);
			}
			catch { /* Process may have exited */ }
		}
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
		//temp to get rid of the camera request - TODO nvm, does not work
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
			if (url.Contains('.') || url.Contains(':') || url.Contains('/')) //TODO: got to fix this
			{
				if (!await TryNavigateAsync("https://" + url))
					if (!await TryNavigateAsync("http://" + url))
						Core.CoreWebView2.Navigate(
							InfoGetter.GetSearchUrl(TabManager.Instance.Cache.CurrentSearchEngine, url));
			}
			else
				Core.CoreWebView2.Navigate(InfoGetter.GetSearchUrl(TabManager.Instance.Cache.CurrentSearchEngine, url));
		}
	}
	
	private async Task<bool> TryNavigateAsync(string url)
	{
		var tcs = new TaskCompletionSource<bool>();

		void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			Core.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
			tcs.TrySetResult(args.IsSuccess || args.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown);
		}

		try
		{
			Core.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
			Core.CoreWebView2.Navigate(url);

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			cts.Token.Register(() => tcs.TrySetResult(false));

			return await tcs.Task;
		}
		catch
		{
			Core.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
			return false;
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
	
	private ObservableCollection<FMenuItem> BaseItems =>
	[
		new() { Text = "Close", Action = () => TabManager.RemoveTab(Id) },
		new() { Text = "Duplicate", Action = () =>
		{
			var groupId = TabManager.Groups.FirstOrDefault(g => g.Tabs.Contains(this))?.Id;
			var id = TabManager.AddTab(Core.Source.ToString());
			TabManager.SwapActiveTabTo(id);
			if (groupId is { } gid)
				TabManager.MoveTabToGroup(id, gid);
			
		} },
		new() { Text = "Move to new group", Action = () => TabManager.MoveTabToGroup(Id, TabManager.CreateGroup().Id) },
	];

	public readonly Func<ObservableCollection<FMenuItem>> GetMenuItems;

	public void UpdateTabManager(TabManager newManager)
	{
		TabManager = newManager;
	}
}