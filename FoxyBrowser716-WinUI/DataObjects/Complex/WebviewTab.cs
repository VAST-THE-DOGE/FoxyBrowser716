using System.Threading;
using Windows.Foundation;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Basic;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716_WinUI.DataObjects.Complex;

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
			Title = "New Tab",
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

		var extensionSetupTask = TabManager.Instance.GetExtensions().ContinueWith(SetupExtensions);

		Core.AllowDrop = true;
		Core.CoreWebView2.DefaultDownloadDialogCornerAlignment = CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
		Core.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
		Core.CoreWebView2.Profile.IsPasswordAutosaveEnabled = true;
		// Core.CoreWebView2.Profile.IsGeneralAutofillEnabled = true;
		Core.CoreWebView2.Settings.AreDevToolsEnabled = true;
		
		Core.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
		Core.CoreWebView2.FaviconChanged += OnFaviconChanged;
		Core.CoreWebView2.SourceChanged += CoreWebView2OnSourceChanged;
		
		await Task.WhenAll(extensionSetupTask, NavigateOrSearch(_startingUrl));
	}

	private void CoreWebView2OnSourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
	{
		Info.Url = Core.CoreWebView2.Source;
	}

	public async Task NavigateOrSearch(string url)
	{
		if (!InitializeTask.IsCompleted)
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

	private void OnFaviconChanged(object sender, object e)
	{
		Info.FavIconUrl = Core.CoreWebView2.FaviconUri ?? "";
	}

	
	private async Task SetupExtensions(Task<List<Extension>> extensionsTask)
	{
		var extensions = await extensionsTask;
		List<Task<CoreWebView2BrowserExtension>> tasks = [];
		tasks.AddRange(
			extensions.Select(ext => Core.CoreWebView2.Profile.AddBrowserExtensionAsync(ext.FolderPath).AsTask())
			);

		await Task.WhenAll(tasks);
	}
}