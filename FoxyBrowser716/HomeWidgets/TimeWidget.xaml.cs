using System.Threading.Tasks;
using System.Timers;

namespace FoxyBrowser716.HomeWidgets;

public partial class TimeWidget : IWidget
{
	public TimeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "TimeWidget";
	public override string WidgetName => StaticWidgetName;

	public override Task Initialize()
	{
		var timer = new System.Timers.Timer(250);
		timer.Elapsed += UpdateTime;
		timer.AutoReset = true;
		timer.Enabled = true;

		return Task.CompletedTask;
	}

	private void UpdateTime(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() => TimeLabel.Text = DateTime.Now.ToString("h:mm:ss tt"));
	}
}