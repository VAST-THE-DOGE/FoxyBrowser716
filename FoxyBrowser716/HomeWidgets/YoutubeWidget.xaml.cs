using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Windows;

namespace FoxyBrowser716.HomeWidgets;

public partial class YoutubeWidget : IWidget
{
	public YoutubeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "YoutubeWidget";
	public override string WidgetName => StaticWidgetName;

	private TabManager _tabManager;
	
	public override Task Initialize(TabManager manager)
	{
		_tabManager = manager;
		
		return Task.CompletedTask;
	}

	private void YoutubeWidgetClick(object sender, RoutedEventArgs routedEventArgs)
	{
		_tabManager.SwapActiveTabTo(_tabManager.AddTab("https://www.youtube.com"));
	}
}