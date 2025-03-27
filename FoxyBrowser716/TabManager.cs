using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716;

/// <summary>
/// A work in progress object that will contain most application data relating to websites and tabs.
/// </summary>
public class TabManager
{
	private readonly ConcurrentDictionary<int, WebsiteTab> _tabs = [];

	/*private readonly ConcurrentDictionary<int, TabInfo> _pins = [];

	private readonly ConcurrentDictionary<int, TabInfo> _bookmarks = [];*/

	private int _pinBookmarkCounter;

	private bool _initialized;
	
	public event Action TabsUpdated;
	/*public event Action PinsUpdated;
	public event Action BookmarksUpdated;*/
	
	/// <summary>
	/// data is formated like this:
	/// <code>(old id, new id)</code>
	/// </summary>
	public event Action<int, int> ActiveTabChanged;
	public int ActiveTabId { get; private set; } = -1;

	private bool _performanceMode = false; //TODO: link into settings?
	
	public static CoreWebView2Environment? WebsiteEnvironment { get; private set; }
	
	public event Action<WebsiteTab> TabCreated;
	public event Action<int> TabRemoved;
	
	/// <summary>
	/// Loads json data for pins and bookmarks.
	/// </summary>
	public async Task InitializeData()
	{
		var options = new CoreWebView2EnvironmentOptions
		{
			AreBrowserExtensionsEnabled = true,
			AllowSingleSignOnUsingOSPrimaryAccount = true
		};
		
		//TODO: User data turns into "{InstanceName}\WebView2Data"
		WebsiteEnvironment ??= await CoreWebView2Environment.CreateAsync(null, "UserData", options);

		_initialized = true; // allow saving pis and bookmarks
	}

	// getter and setters
	public WebsiteTab? GetTab(int tabId) => _tabs.GetValueOrDefault(tabId);
	
	public Dictionary<int,WebsiteTab> GetAllTabs() => _tabs.ToDictionary();
	
	public void RemoveTab(int tabId, bool keepCore = false) {
		if (_tabs.Count > 1)
		{
			var newId = ActiveTabId == tabId ? _tabs.First(t => t.Key != tabId).Key : ActiveTabId;
			SwapActiveTabTo(newId);
		}
		else
		{
			SwapActiveTabTo(-1);
		}
		
		if (_tabs.TryRemove(tabId, out var tab))
			if(!keepCore)
				tab.TabCore.Dispose();
			else if (tab.TabCore.Parent is Grid g)
				g.Children.Remove(tab.TabCore);
				
		TabsUpdated?.Invoke();
		TabRemoved?.Invoke(tabId);
	}
	
	public int AddTab(string url)
	{
		var tab = new WebsiteTab(url);
		_tabs.TryAdd(tab.TabId, tab);
		TabCreated?.Invoke(tab);
		TabsUpdated?.Invoke();
		return tab.TabId;
	}

	// tab management
	private int? nextToSwap;
	private bool swaping;
	public void SwapActiveTabTo(int tabId)
	{
		if (ActiveTabId == tabId)
			return;
		
		if (swaping)
		{
			nextToSwap = tabId;
			return;
		}
		swaping = true;
		
		if (tabId != -1 && _tabs.TryGetValue(tabId, out var tab))
		{
			tab.SetupTask.ContinueWith(_ =>
			{
				if (tab.TabCore.CoreWebView2.IsSuspended)
					tab.TabCore.CoreWebView2.Resume();
			}, TaskScheduler.FromCurrentSynchronizationContext());
		} else if (tabId != -1)
		{
			// should not happen, return.
			swaping = false;
			return;
		}
		
		var oldId = ActiveTabId;
		ActiveTabId = tabId;
		
		if (_performanceMode)
		{
			List<Task> initTasks = [];

			async Task Suspend(KeyValuePair<int, WebsiteTab> idTabPair)
			{
				await idTabPair.Value.SetupTask;
				await idTabPair.Value.TabCore.EnsureCoreWebView2Async(WebsiteEnvironment);
				if (idTabPair.Value.TabCore.CoreWebView2.IsDocumentPlayingAudio)
					return;
				await idTabPair.Value.TabCore.CoreWebView2.TrySuspendAsync();
			}

			// call the above function with every tab that is not the active tab and add the task to a list.
			initTasks.AddRange(_tabs.Where(t => t.Value.TabId != tabId).Select(Suspend));
			Task.WaitAll(initTasks);
		}
		
		foreach (var t in _tabs)
			t.Value.TabCore.Visibility = t.Value.TabId == ActiveTabId
				? Visibility.Visible
				: Visibility.Collapsed;
		
		ActiveTabChanged?.Invoke(oldId, ActiveTabId);

		swaping = false;
		if (nextToSwap is { } next)
		{
			nextToSwap = null;
			SwapActiveTabTo(next);
		}
	}
	
	/// <summary>
	/// to be called by another tab manager
	/// </summary>
	/// <param name="tab">tab to add</param>
	public async Task<int> TransferTab(WebsiteTab tab)
	{
		_tabs.TryAdd(tab.TabId, tab); //TODO: is this an issue? might be later on, not now.
		TabCreated?.Invoke(tab);
		TabsUpdated?.Invoke();
		
		return tab.TabId;
	}
}