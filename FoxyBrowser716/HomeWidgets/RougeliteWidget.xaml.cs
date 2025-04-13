using System.Threading.Tasks;
using System.Timers;
using System.Windows;
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
		canvas.
		// var timer = new System.Timers.Timer(50);
		// timer.Elapsed += UpdateTime;
		// timer.AutoReset = true;
		// timer.Enabled = true;

		return Task.CompletedTask;
	}

	// private void UpdateTime(object? sender, ElapsedEventArgs elapsedEventArgs)
	// {
	// 	Dispatcher.Invoke(() => RougeTimeLabel.Text = DateTime.Now.ToString("h:mm:ss tt"));
	// }
}