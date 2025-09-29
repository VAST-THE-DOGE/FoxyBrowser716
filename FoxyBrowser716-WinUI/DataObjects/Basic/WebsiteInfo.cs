using FoxyBrowser716_WinUI.DataObjects.Complex;

namespace FoxyBrowser716_WinUI.DataObjects.Basic;

public partial class WebsiteInfo : ObservableObject
{
	[ObservableProperty] private string _url;
	[ObservableProperty] private string _favIconUrl;
	[ObservableProperty] private string _title;
	[ObservableProperty] private string _note;
	[ObservableProperty] private DateTime? _dateAdded;
}