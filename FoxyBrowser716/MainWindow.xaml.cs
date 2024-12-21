using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Material.Icons;
using Material.Icons.WPF;
using static FoxyBrowser716.ColorPalette;

namespace FoxyBrowser716;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	private Control[] _normalButtons =>
	[
		ButtonMaximize,
		ButtonMinimize,
		ButtonMenu,
		ButtonSidePanel,
		SearchButton,
		TabHome,
		ButtonPin,
		ButtonBookmark
	];
	
	public MainWindow()
	{
		InitializeComponent();
		// BackImage.Source = Material.Icons.MaterialIconDataProvider.GetData(Material.Icons.MaterialIconKind.Close);

		foreach (var button in _normalButtons)
		{
			button.MouseEnter += (_, _) =>
			{
				ChangeColorAnimation(button.Background, MainColor, AccentColor);
			};
			button.MouseLeave += (_, _) =>
			{
				ChangeColorAnimation(button.Background, AccentColor, MainColor);
			};
		}

		SearchBox.GotKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, Colors.White, HighlightColor);
		};
		SearchBox.LostKeyboardFocus += (_, _) =>
		{
			ChangeColorAnimation(SearchBackground.BorderBrush, HighlightColor, Colors.White);
		};
		
		ButtonClose.MouseEnter += (_, _) =>
		{
			ChangeColorAnimation(ButtonClose.Background, MainColor, Colors.Red);
		};
		ButtonClose.MouseLeave += (_, _) =>
		{
			ChangeColorAnimation(ButtonClose.Background, Colors.Red, MainColor);
		};
		StateChanged += Window_StateChanged;
		Window_StateChanged(null, EventArgs.Empty);
	}

	private void Window_StateChanged(object? sender, EventArgs e)
	{
		ButtonMaximize.Content = WindowState == WindowState.Maximized ? new MaterialIcon { Kind = MaterialIconKind.FullscreenExit } :
			new MaterialIcon { Kind = MaterialIconKind.Fullscreen };
	}
	
	private static void ChangeColorAnimation(Brush brush, Color from, Color to, double time=0.2)
	{
		var colorAnimation = new ColorAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(TimeSpan.FromSeconds(time)),
			EasingFunction = new QuadraticEase()
		};
		brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
	}
	
	private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}
		else
		{
			DragMove();
		}
	}

	private void Minimize_Click(object sender, RoutedEventArgs e)
	{
		WindowState = WindowState.Minimized;
	}

	private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
	{
		WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}