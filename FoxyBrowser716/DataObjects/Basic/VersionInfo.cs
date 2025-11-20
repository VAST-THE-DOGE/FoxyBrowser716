namespace FoxyBrowser716.DataObjects.Basic;

public partial class VersionInfo : ObservableObject
{
	[ObservableProperty] public partial string? Version { get; set; }
	[ObservableProperty] public partial string? Timestamp { get; set; }
}