using System.Timers;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class TimeWidget : Widget
{
	public TimeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "TimeWidget";
	public override string WidgetName => StaticWidgetName;

	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings = null)
	{
		base.Initialize(manager, settings);
		
		var timer = new System.Timers.Timer(100);
		timer.Elapsed += UpdateTime;
		timer.AutoReset = true;
		timer.Enabled = true;

		return Task.CompletedTask;
	}

	private void UpdateTime(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			TimeLabel.Text = DateTime.Now.ToString("h:mm:ss tt");
		});
	}
}