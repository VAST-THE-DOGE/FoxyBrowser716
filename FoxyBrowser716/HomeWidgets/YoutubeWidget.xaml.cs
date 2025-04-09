using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Windows;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

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
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		
		_tabManager = manager;
		
		return Task.CompletedTask;
	}

	private void YoutubeWidgetClick(object sender, RoutedEventArgs routedEventArgs)
	{
		_tabManager.SwapActiveTabTo(_tabManager.AddTab("https://www.youtube.com"));
	}
}