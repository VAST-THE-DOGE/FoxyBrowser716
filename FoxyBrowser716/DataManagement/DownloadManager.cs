namespace FoxyBrowser716.DataManagement;

public class DownloadManager
{
	public event Action<Download>? DownloadStarted;
	public event Action<Download>? DownloadCompleted;
	
	public async Task RegisterDownloadManagerToWebView(WebView2 webView2)
	{
		webView2.CoreWebView2.DownloadStarting += async (sender, args) =>
		{
			
		};
	}
}