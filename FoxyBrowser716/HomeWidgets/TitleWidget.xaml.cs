using System.Threading.Tasks;

namespace FoxyBrowser716.HomeWidgets;

public partial class TitleWidget : IWidget
{
	public TitleWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "TitleWidget";
	public override string WidgetName => StaticWidgetName;

	public override Task Initialize(TabManager manager)
	{
		return Task.CompletedTask;
	}
}