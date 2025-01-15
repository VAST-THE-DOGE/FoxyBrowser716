using System.Threading.Tasks;

namespace FoxyBrowser716.HomeWidgets;

public partial class TitleWidget : IWidget
{
	public TitleWidget()
	{
		InitializeComponent();
	}

	public override string WidgetName => "TitleWidget";

	public override int MinWidgetWidth => 1;
	public override int MinWidgetHeight => 1;
	public override int MaxWidgetWidth => 40;
	public override int MaxWidgetHeight => 20;

	public override Task Initialize()
	{
		return Task.CompletedTask;
	}
}