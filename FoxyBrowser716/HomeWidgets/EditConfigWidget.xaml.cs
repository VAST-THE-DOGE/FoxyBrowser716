using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using static FoxyBrowser716.Animator;
using static FoxyBrowser716.ColorPalette;

namespace FoxyBrowser716.HomeWidgets;

public partial class EditConfigWidget : IWidget
{
	public EditConfigWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "EditConfigWidget";
	public override string WidgetName => StaticWidgetName;
	
	public override bool CanRemove => false;
	public override bool CanAdd => false;
	
	internal event Action Clicked;
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings = null)
	{
		EditConfigButton.MouseEnter += (_, _) => { ChangeColorAnimation(EditConfigButton.Background, Transparent, AccentColor); };
		EditConfigButton.MouseLeave += (_, _) => { ChangeColorAnimation(EditConfigButton.Background, AccentColor, Transparent); };
		EditConfigButton.PreviewMouseLeftButtonDown += (_, _) => { EditConfigButton.Foreground = new SolidColorBrush(HighlightColor); };
		EditConfigButton.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(EditConfigButton.Foreground, HighlightColor, Colors.White);
			Clicked?.Invoke();
		};
		
		return Task.CompletedTask;
	}
}