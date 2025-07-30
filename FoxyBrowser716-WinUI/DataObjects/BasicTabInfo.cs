namespace FoxyBrowser716_WinUI.DataObjects;

public class BasicTabInfo : NotifyPropertyChanged
{
	public string Url { get; init; }
	public string FaviconUrl { get; init; }
	public string Title { get; init; }
	public DateTime DateAdded { get; init; }
}