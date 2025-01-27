using System.Threading.Tasks;
using System.Timers;

namespace FoxyBrowser716.HomeWidgets;

public partial class TimeDateWidget : IWidget
{
	public TimeDateWidget()
	{
		InitializeComponent();
	}

	public override string WidgetName => "TimeDateWidget";

	public override int MinWidgetWidth => 1;
	public override int MinWidgetHeight => 1;
	public override int MaxWidgetWidth => 40;
	public override int MaxWidgetHeight => 20;

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