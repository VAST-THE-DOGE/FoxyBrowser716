using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;

[ObservableObject]
public partial class InstanceCache
{
	[ObservableProperty]
	private InfoGetter.SearchEngine _currentSearchEngine;
	
	[ObservableProperty]
	private bool _leftBarLocked;
}