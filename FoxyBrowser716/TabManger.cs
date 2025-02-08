using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716;

/// <summary>
/// A work in progress object that will contain most application data relating to websites and tabs.
/// </summary>
public class TabManger
{
	private readonly ConcurrentDictionary<int, WebsiteTab> _tabs = [];

	private readonly ConcurrentDictionary<int, TabInfo> _pins = [];

	private readonly ConcurrentDictionary<int, TabInfo> _bookmarks = [];

	private int _pinBookmarkCounter;

	public event Action TabsUpdated;
	public event Action PinsUpdated;
	public event Action BookmarksUpdated;
	
	private event Action<int> ActiveTabChanged;
	public int ActiveTabId { get; private set; }

	private bool _performanceMode = false; //TODO: link into settings?
	
	public static CoreWebView2Environment? WebsiteEnvironment { get; private set; }
	
	public event Action<WebsiteTab> TabCreated;
	
	/// <summary>
	/// Loads json data for pins and bookmarks.
	/// </summary>
	public async Task InitializeData()
	{
		//TODO
	}

	// getter and setters
	private WebsiteTab? GetTab(int tabId) => _tabs.GetValueOrDefault(tabId);
	private WebsiteTab? GetPin(int pinId) => _tabs.GetValueOrDefault(pinId);
	private WebsiteTab? GetBookmark(int bookmarkId) => _tabs.GetValueOrDefault(bookmarkId);
	
	private IEnumerable<WebsiteTab> GetAllTabs() => _tabs.Values;
	private IEnumerable<TabInfo> GetAllPins() => _pins.Values;
	private IEnumerable<TabInfo> GetAllBookmarks() => _bookmarks.Values;
	
	private void RemoveTab(int tabId) {
		if(_tabs.TryRemove(tabId, out var tab))
		{
			if (_tabs.Count > 1)
			{
				var newId = _tabs.First(t => t.Key != tabId).Key;
				SwapActiveTabTo(newId);
			}
			else
			{
				SwapActiveTabTo(-1);
			}
			
			tab.TabCore.Dispose(); //TODO: need to test, could cause errors
			TabsUpdated?.Invoke();
		}
	}
	private void RemovePin(int tabId) {
		if(_pins.TryRemove(tabId, out _))
			PinsUpdated?.Invoke();
	}
	private void RemoveBookmark(int tabId) {
		if(_bookmarks.TryRemove(tabId, out _))
			BookmarksUpdated?.Invoke();
	}
	
	private int AddTab(string url)
	{
		var tab = new WebsiteTab(url);
		_tabs.TryAdd(tab.TabId, tab);
		TabCreated?.Invoke(tab);
		TabsUpdated?.Invoke();
		return tab.TabId;
	}

	private int AddPin(WebsiteTab tab) => AddPin(new TabInfo { Title = tab.Title, Url = tab.TabCore.Source.ToString(), Image = tab.Icon });
	private int AddPin(TabInfo tab)
	{
		var key = Interlocked.Increment(ref _pinBookmarkCounter);
		_pins.TryAdd(key, tab);
		PinsUpdated?.Invoke();
		return key;
	}
	
	private int AddBookmark(WebsiteTab tab) => AddBookmark(new TabInfo { Title = tab.Title, Url = tab.TabCore.Source.ToString(), Image = tab.Icon });
	private int AddBookmark(TabInfo tab)
	{
		var key = Interlocked.Increment(ref _pinBookmarkCounter);
		_bookmarks.TryAdd(key, tab);
		BookmarksUpdated?.Invoke();
		return key;
	}

	// tab management
	private int? nextToSwap;
	private bool swaping;
	private void SwapActiveTabTo(int tabId)
	{
		if (swaping)
		{
			nextToSwap = tabId;
			return;
		}

		swaping = true;
		
		if (tabId != -1 && _tabs.TryGetValue(tabId, out var tab))
		{
			ActiveTabId = tabId;
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
		
		if (_performanceMode)
		{
			List<Task> initTasks = [];

			async Task Suspend(KeyValuePair<int, WebsiteTab> idTabPair)
			{
				await idTabPair.Value.TabCore.EnsureCoreWebView2Async(WebsiteEnvironment);
				if (idTabPair.Value.TabCore.CoreWebView2.IsDocumentPlayingAudio)
					return;
				idTabPair.Value.TabCore.CoreWebView2.TrySuspendAsync();
			}

			// call the above function with every tab that is not the active tab and add the task to a list.
			initTasks.AddRange(_tabs.Where(t => t.Value.TabId != tabId).Select(Suspend));
			Task.WaitAll(initTasks);
		}
		
		foreach (var t in _tabs)
			t.Value.TabCore.Visibility = t.Value.TabId == ActiveTabId
				? Visibility.Visible
				: Visibility.Collapsed;
		
		ActiveTabChanged?.Invoke(ActiveTabId);

		swaping = false;
		if (nextToSwap is { } next)
		{
			nextToSwap = null;
			SwapActiveTabTo(next);
		}
	}
}