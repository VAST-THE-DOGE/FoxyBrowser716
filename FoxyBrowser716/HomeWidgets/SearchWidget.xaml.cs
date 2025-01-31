using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static FoxyBrowser716.MainWindow;
using static FoxyBrowser716.ColorPalette;

namespace FoxyBrowser716.HomeWidgets;

public partial class SearchWidget : IWidget
{
	// custom event, more advanced logic needed in HomePage.xaml.cs.
	public event Action<string> OnSearch;
	
	public SearchWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "SearchWidget";
	public override string WidgetName => StaticWidgetName;
	public override int MaxWidgetHeight => 10;

	public override Task Initialize()
	{
		SearchBox.GotKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, Colors.White, HighlightColor);
		};
		SearchBox.LostKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, HighlightColor, Colors.White);
		};
		
		SearchButton.MouseEnter += (_, _) => { ChangeColorAnimation(SearchButton.Background, MainColor, AccentColor); };
		SearchButton.MouseLeave += (_, _) => { ChangeColorAnimation(SearchButton.Background, AccentColor, MainColor); };
		SearchButton.PreviewMouseLeftButtonDown += (_, _) => { SearchButton.Foreground = new SolidColorBrush(HighlightColor); };
		SearchButton.PreviewMouseLeftButtonUp += (_, _) =>
		{
			ChangeColorAnimation(SearchButton.Foreground, HighlightColor, Colors.White);
		};
		
		return Task.CompletedTask;
	}

	private void SearchClick(object sender, RoutedEventArgs routedEventArgs)
	{
		OnSearch?.Invoke(SearchBox.Text);
	}
}