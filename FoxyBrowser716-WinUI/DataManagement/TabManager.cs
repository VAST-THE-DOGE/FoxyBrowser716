using System.Collections.Concurrent;
using System.Diagnostics;
using FoxyBrowser716_WinUI.DataObjects.Complex;
using Microsoft.Web.WebView2.Core;

namespace FoxyBrowser716_WinUI.DataManagement;

public class TabManager
{
	public Instance Instance { get; private set; }

	private ConcurrentDictionary<int, WebviewTab> _tabs { get; set; } = [];
	
	public event Action<WebviewTab> TabAdded;
	public event Action<WebviewTab> TabRemoved;
	public event Action<(int oldId, int newId)> ActiveTabChanged; 
	
	public int ActiveTabId { get; private set; } = -1;
	
	public CoreWebView2Environment? WebsiteEnvironment { get; private set; }

	private TabManager()
	{ }

	public static async Task<TabManager> Create(Instance instance)
	{
		var newManager = new TabManager();
		await newManager.Initialize(instance);
		return newManager;
	}

	private async Task Initialize(Instance instance)
	{
		Instance = instance;
		
		var options = new CoreWebView2EnvironmentOptions
		{
			AreBrowserExtensionsEnabled = true,
			AllowSingleSignOnUsingOSPrimaryAccount = true,
		};
		WebsiteEnvironment ??= await CoreWebView2Environment.CreateWithOptionsAsync(null, FoxyFileManager.BuildFolderPath(FoxyFileManager.FolderType.WebView2, Instance.Name), options);
	}

	public bool TryGetTab(int tabId, out WebviewTab? tab)
	{
		if (_tabs.GetValueOrDefault(tabId) is { } t)
		{
			tab = t;
			return true;
		}
		else
		{
			tab = null;
			return false;
		}
	}
	
	public Dictionary<int,WebviewTab> GetAllTabs() => _tabs.ToDictionary();
	
	public void RemoveTab(int tabId) {
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
		{
			if (tab.Core.Parent is Grid g)
			{
				g.Children.Remove(tab.Core);
			}			
			else
				throw new Exception($"Tab (Id = {tabId}) could not be removed. Unknown Core Parent.");
			TabRemoved?.Invoke(tab);
		}
	}
	
	public int AddTab(string? url)
	{
		var tab = new WebviewTab(this, url);
		_tabs.TryAdd(tab.Id, tab);
		TabAdded?.Invoke(tab);
		return tab.Id;
	}

	// tab management
	private int? _nextToSwap;
	private bool _swaping;
	public void SwapActiveTabTo(int tabId) => _ = SwapActiveTabTo(tabId, true);
	
	public async Task SwapActiveTabTo(int tabId, bool waitForTabToInitialize)
	{
		Debug.WriteLine($"{ActiveTabId} => {tabId}");
		if (ActiveTabId == tabId)
			return;

		if (waitForTabToInitialize && ActiveTabId >= 0 && _tabs.TryGetValue(ActiveTabId, out var activeTab))
		{
			await activeTab.InitializeTask;
		}
		
		if (_swaping)
		{
			_nextToSwap = tabId;
			return;
		}
		_swaping = true;
		
		if (tabId >= 0 && _tabs.TryGetValue(tabId, out var tab))
		{
			await tab.InitializeTask;
			if (tab.Core.CoreWebView2.IsSuspended)
				tab.Core.CoreWebView2.Resume();
			tab.Core.Focus(FocusState.Programmatic);
			
		} else if (tabId >= 0)
		{
			// should not happen, return.
			_swaping = false;
			return;
		}
		
		var oldId = ActiveTabId;
		ActiveTabId = tabId;
		
		if (false) //TODO
		{
			List<Task> initTasks = [];

			async Task Suspend(KeyValuePair<int, WebviewTab> idTabPair)
			{
				await idTabPair.Value.InitializeTask;
				await idTabPair.Value.Core.EnsureCoreWebView2Async(WebsiteEnvironment);
				if (idTabPair.Value.Core.CoreWebView2.IsDocumentPlayingAudio)
					return;
				await idTabPair.Value.Core.CoreWebView2.TrySuspendAsync();
			}

			// call the above function with every tab that is not the active tab and add the task to a list.
			initTasks.AddRange(_tabs.Where(t => t.Value.Id != tabId).Select(Suspend));
			Task.WaitAll(initTasks);
		}
		
		foreach (var t in _tabs)
			t.Value.Core.Visibility = t.Value.Id == ActiveTabId
				? Visibility.Visible
				: Visibility.Collapsed;
		
		ActiveTabChanged?.Invoke((oldId, ActiveTabId));

		_swaping = false;
		if (_nextToSwap is { } next)
		{
			_nextToSwap = null;
			SwapActiveTabTo(next);
		}
	}
}