using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using static FoxyBrowser716.Animator;
using static FoxyBrowser716.ColorPalette;

namespace FoxyBrowser716.HomeWidgets;

public partial class SearchWidget : IWidget
{
	public SearchWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "SearchWidget";
	public override string WidgetName => StaticWidgetName;
	public override int MaxWidgetHeight => 10;

	public override Dictionary<string, IWidgetSetting>? WidgetSettings { get; set; } =
		new()
		{
			["DefaultBrowser"]=new WidgetSettingString("google"),
		};

	private TabManager _tabManager;

	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings = null)
	{
		base.Initialize(manager, settings);
		
		_tabManager = manager;
		
		SearchBox.GotKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, Colors.White, HighlightColor);
		};
		SearchBox.LostKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, HighlightColor, Colors.White);
		};
		SearchBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) SearchClick(this, EventArgs.Empty); };
		
		SearchButton.MouseEnter += (_, _) => { ChangeColorAnimation(SearchButton.Background, Transparent, AccentColor); };
		SearchButton.MouseLeave += (_, _) => { ChangeColorAnimation(SearchButton.Background, AccentColor, Transparent); };
		SearchButton.PreviewMouseLeftButtonUp += (_, _) => { ChangeColorAnimation(SearchButton.Foreground, HighlightColor, Colors.White); };
		SearchButton.PreviewMouseLeftButtonDown += (_, _) => { SearchButton.Foreground = new SolidColorBrush(HighlightColor); };
		
		return Task.CompletedTask;
	}

	private void SearchClick(object sender, EventArgs routedEventArgs)
	{
		_tabManager.SwapActiveTabTo(_tabManager.AddTab(SearchBox.Text));
	}
}