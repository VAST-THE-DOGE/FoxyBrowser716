namespace FoxyBrowser716_WinUI.DataObjects;

public class WebsiteInfo
{
	public required string Url { get; init; }
	public required string FavIconUrl { get; init; }
	public required string Title { get; init; }
	public DateTime DateAdded { get; init; }
}