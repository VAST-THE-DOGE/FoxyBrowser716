using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;

public partial class WebsiteInfo : ObservableObject
{
	[ObservableProperty] public partial string Url { get; set; } = string.Empty;
	[ObservableProperty] public partial string FavIconUrl { get; set; } = string.Empty;
	[ObservableProperty] public partial string Title { get; set; } = string.Empty;
	[ObservableProperty] public partial string Note { get; set; } = string.Empty;
	[ObservableProperty] public partial DateTime? DateAdded { get; set; }
}