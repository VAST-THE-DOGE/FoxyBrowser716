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

	public override string WidgetName => "YoutubeWidget";

	public override int MinWidgetWidth => 1;
	public override int MinWidgetHeight => 1;
	public override int MaxWidgetWidth => 40;
	public override int MaxWidgetHeight => 20;

	public override Task Initialize()
	{
		return Task.CompletedTask;
	}

	private void YoutubeWidgetClick(object sender, RoutedEventArgs routedEventArgs)
	{
		GoToYoutube?.Invoke("https://www.youtube.com");
	}
}