using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Windows;

namespace FoxyBrowser716.HomeWidgets;

public partial class YoutubeWidget : IWidget
{
	public event Action<string> GoToYoutube;
	public YoutubeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "YoutubeWidget";
	public override string WidgetName => StaticWidgetName;

	public override Task Initialize()
	{
		return Task.CompletedTask;
	}

	private void YoutubeWidgetClick(object sender, RoutedEventArgs routedEventArgs)
	{
		GoToYoutube?.Invoke("https://www.youtube.com");
	}
}