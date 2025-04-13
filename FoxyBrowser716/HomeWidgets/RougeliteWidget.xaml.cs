using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class RougeWidget : IWidget
{
	public RougeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "RougeWidget";
	public override string WidgetName => StaticWidgetName;
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		var timer = new System.Timers.Timer(30);
		timer.Elapsed += Tick;
		timer.AutoReset = true;
		timer.Enabled = true;
		
		return Task.CompletedTask;
	}

	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			
		});
	}
}