using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;

public partial class WebsiteInfo : ObservableObject
{
	[ObservableProperty] private string _url;
	[ObservableProperty] private string _favIconUrl;
	[ObservableProperty] private string _title;
	[ObservableProperty] private string _note;
	[ObservableProperty] private DateTime? _dateAdded;
}