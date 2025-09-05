namespace FoxyBrowser716_WinUI.Controls.HomePage.Widgets;

[WidgetInfo("Example Widget", MaterialIconKind.PuzzleEdit, WidgetCategory.Misc)]
public partial class ExampleWidget : WidgetBase
{
	protected ExampleWidget()
	{
		InitializeComponent();
	}

    protected override async Task Initialize()
    {
       // Initialize any data, time, or anything else when created
    }

    protected override void ApplyTheme()
    {
        // Apply theme to ui elements here
    }
}
