using System.Collections.Concurrent;
using System.Windows.Controls;

namespace FoxyBrowser716;

public class WebsiteInfoList
{
	private readonly ConcurrentDictionary<int, TabInfo> _tabinfos = [];

	private string? _filePath;
	
	private int _counter;
	
	public event Action<int, TabInfo> TabInfoAdded;
	public event Action<int, TabInfo> TabInfoRemoved;

	public async Task LoadTabInfoFromJson(string filePath)
	{
		var tabInfos = await TabInfo.TryLoadTabs(filePath);

		foreach (var t in tabInfos)
		{
			_tabinfos.TryAdd(_counter++, t);
		}
	}
	
	public void RemoveTabInfo(int tabId)
	{
		if (!_tabinfos.TryRemove(tabId, out var tabInfo)) return;
		
		TabInfoRemoved?.Invoke(tabId, tabInfo);
		
		if (_filePath is { } filePath)
			Task.WhenAll(TabInfo.SaveTabs(filePath, _tabinfos.Values.ToArray()));
	}
	
	public int AddTabInfo(WebsiteTab tab) => AddTabInfo(new TabInfo { Title = tab.Title, Url = tab.TabCore.Source.ToString(), Image = new Image { Source = tab.Icon.Source } });
	public int AddTabInfo(TabInfo tab)
	{
		var key = Interlocked.Increment(ref _counter);
		_tabinfos.TryAdd(key, tab);
		TabInfoAdded?.Invoke(key, tab);
		if (_filePath is { } filePath)
			Task.WhenAll(TabInfo.SaveTabs(filePath, _tabinfos.Values.ToArray()));
		return key;
	}
	
	public TabInfo? GetTabInfo(int tabInfoId) => _tabinfos.GetValueOrDefault(tabInfoId);
	
	public Dictionary<int, TabInfo> GetAllTabInfos() => _tabinfos.ToDictionary();
}