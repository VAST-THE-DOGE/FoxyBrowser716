using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class DateWidget : IWidget
{
	public DateWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "DateWidget";
	public override string WidgetName => StaticWidgetName;

	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		
		// set the time immediately
		TimeLabel.Text = DateTime.Now.ToString("M/d/yy");
		
		var timer = new System.Timers.Timer(10000);
		timer.Elapsed += UpdateTime;
		timer.AutoReset = true;
		timer.Enabled = true;

		return Task.CompletedTask;
	}

	private void UpdateTime(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() => TimeLabel.Text = DateTime.Now.ToString("M/d/yy"));
	}
}