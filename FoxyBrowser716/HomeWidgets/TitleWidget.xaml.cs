using System.Threading.Tasks;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class TitleWidget : IWidget
{
	public TitleWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "TitleWidget";
	public override string WidgetName => StaticWidgetName;
}