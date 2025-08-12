using FoxyBrowser716_WinUI.DataObjects.Complex;

namespace FoxyBrowser716_WinUI.DataObjects.Basic;

[ObservableObject]
public partial class InstanceCache
{
	[ObservableProperty]
	private InfoGetter.SearchEngine _currentSearchEngine;
	
	[ObservableProperty]
	private bool _leftBarLocked;
}