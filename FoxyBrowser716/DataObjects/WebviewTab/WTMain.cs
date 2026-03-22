namespace FoxyBrowser716.DataObjects.Complex;

[ObservableObject]
public partial class NewWebviewTab : Grid
{
	[ObservableProperty] public partial WebsiteInfo Info { get; set; }
	[ObservableProperty] public partial bool IsActive { get; set; }
	
	[ObservableProperty] public partial bool CanGoForward { get; set; }
	[ObservableProperty] public partial bool CanGoBack { get; set; }
	[ObservableProperty] public partial bool TabLoading { get; set; }
	[ObservableProperty] public partial bool IsPlayingAudio { get; set; }
	
	public bool IsMuted { get; set; }

	
	// [ObservableProperty] public partial TODO TODO { get; set; }
	// [ObservableProperty] public partial TODO TODO { get; set; }
	// [ObservableProperty] public partial TODO TODO { get; set; }
	// [ObservableProperty] public partial TODO TODO { get; set; }

	public NewWebviewTab()
	{
		
	}
}