namespace FoxyBrowser716.HomeWidgets;

public partial class TitleWidget : Widget
{
	public TitleWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "TitleWidget";
	public override string WidgetName => StaticWidgetName;
}