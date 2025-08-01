using FoxyBrowser716_WinUI.DataObjects.Complex;

namespace FoxyBrowser716_WinUI.DataObjects.Basic;

public class WebsiteInfo
{
	public required string Url { get; set; }
	public required string FavIconUrl { get; set; }
	public required string Title { get; set; }
	public DateTime? DateAdded { get; set; }
}