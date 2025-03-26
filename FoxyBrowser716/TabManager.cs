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
	
	/*public event Action<int, TabInfo> PinCreated;
	public event Action<int> PinRemoved;
	
	public event Action<int, TabInfo> BookmarkCreated;
	public event Action<int> BookmarkRemoved;
	*/
	
	/// <summary>
	/// Loads json data for pins and bookmarks.
	/// </summary>
	public async Task InitializeData()
	{
		// var loadPinTask = TabInfo.TryLoadTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"));

		var options = new CoreWebView2EnvironmentOptions();
		options.AreBrowserExtensionsEnabled = true;
		options.AllowSingleSignOnUsingOSPrimaryAccount = true;
		WebsiteEnvironment ??= await CoreWebView2Environment.CreateAsync(null, "UserData", options);
		
		/*var pins = await loadPinTask;
		foreach (var pin in pins)
		{
			AddPin(pin);
		}*/

		_initialized = true; // allow saving pis and bookmarks
	}

	// getter and setters
	public WebsiteTab? GetTab(int tabId) => _tabs.GetValueOrDefault(tabId);
	//public TabInfo? GetPin(int pinId) => _pins.GetValueOrDefault(pinId);
	//public TabInfo? GetBookmark(int bookmarkId) => _bookmarks.GetValueOrDefault(bookmarkId);
	
	public Dictionary<int,WebsiteTab> GetAllTabs() => _tabs.ToDictionary();
	//public Dictionary<int, TabInfo> GetAllPins() => _pins.ToDictionary();
	//public Dictionary<int,TabInfo> GetAllBookmarks() => _bookmarks.ToDictionary();
	
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
	/*public void RemovePin(int tabId)
	{
		if (!_pins.TryRemove(tabId, out _)) return;
		
		PinsUpdated?.Invoke();
		PinRemoved?.Invoke(tabId);
		
		if (!_initialized) return;
		Task.WhenAll(TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"), _pins.Values.ToArray()));
	}
	public void RemoveBookmark(int tabId)
	{
		if (!_bookmarks.TryRemove(tabId, out _)) return;
		
		BookmarksUpdated?.Invoke();
		BookmarkRemoved?.Invoke(tabId);
		
		if (!_initialized) return;
		Task.WhenAll(TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"), _bookmarks.Values.ToArray()));

	}*/
	
	public int AddTab(string url)
	{
		var tab = new WebsiteTab(url);
		_tabs.TryAdd(tab.TabId, tab);
		TabCreated?.Invoke(tab);
		TabsUpdated?.Invoke();
		return tab.TabId;
	}

	/*public int AddPin(WebsiteTab tab) => AddPin(new TabInfo { Title = tab.Title, Url = tab.TabCore.Source.ToString(), Image = new Image { Source = tab.Icon.Source } });
	public int AddPin(TabInfo tab)
	{
		var key = Interlocked.Increment(ref _pinBookmarkCounter);
		_pins.TryAdd(key, tab);
		PinsUpdated?.Invoke();
		PinCreated?.Invoke(key, tab);
		if (_initialized)
			Task.WhenAll(TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pins.json"), _pins.Values.ToArray()));
		return key;
	}
	
	public int AddBookmark(WebsiteTab tab) => AddBookmark(new TabInfo { Title = tab.Title, Url = tab.TabCore.Source.ToString(), Image = tab.Icon });
	public int AddBookmark(TabInfo tab)
	{
		var key = Interlocked.Increment(ref _pinBookmarkCounter);
		_bookmarks.TryAdd(key, tab);
		BookmarksUpdated?.Invoke();
		BookmarkCreated?.Invoke(key, tab);
		if (_initialized)
			Task.WhenAll(TabInfo.SaveTabs(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bookmarks.json"), _bookmarks.Values.ToArray()));
		return key;
	}*/

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
	/// to be called with the other tab manager as a parameter to move a tab
	/// </summary>
	/// <param name="otherManager">manager to send the Tab to</param>
	/// <param name="tab">tab to move</param>
	public async Task TransferTab(TabManager otherManager, WebsiteTab tab)
	{
		if (otherManager == this) throw new ArgumentException("can't move tab to the same manager");
		
		RemoveTab(tab.TabId, true);
		await otherManager.TransferTab(tab);
	}
	
	/// <summary>
	/// to be called by another tab manager
	/// </summary>
	/// <param name="tab">tab to add</param>
	private async Task TransferTab(WebsiteTab tab)
	{
		_tabs.TryAdd(tab.TabId, tab); //TODO: is this an issue? might be later on, not now.
		TabCreated?.Invoke(tab);
		TabsUpdated?.Invoke();
	}
}