using CommunityToolkit.Mvvm.ComponentModel;
using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;


[ObservableObject]

public partial class VersionInfo
{
	[ObservableProperty] 
	private string? _version;
}