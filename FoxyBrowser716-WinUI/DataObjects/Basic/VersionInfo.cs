using CommunityToolkit.Mvvm.ComponentModel;
using FoxyBrowser716_WinUI.DataObjects.Complex;

namespace FoxyBrowser716_WinUI.DataObjects.Basic;


[ObservableObject]

public partial class VersionInfo
{
	[ObservableProperty] 
	private string? _version;
}