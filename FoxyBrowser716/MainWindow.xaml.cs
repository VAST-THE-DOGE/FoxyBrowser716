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
	];
	
	public MainWindow()
	{
		InitializeComponent();
		// BackImage.Source = Material.Icons.MaterialIconDataProvider.GetData(Material.Icons.MaterialIconKind.Close);

		foreach (var button in _normalButtons)
		{
			button.MouseEnter += (_, _) =>
			{
				ChangeBackColorAnimation(button, MainColor, AccentColor);
			};
			button.MouseLeave += (_, _) =>
			{
				ChangeBackColorAnimation(button, AccentColor, MainColor);
			};
		}
		
		ButtonClose.MouseEnter += (_, _) =>
		{
			ChangeBackColorAnimation(ButtonClose, MainColor, Colors.Red);
		};
		ButtonClose.MouseLeave += (_, _) =>
		{
			ChangeBackColorAnimation(ButtonClose, Colors.Red, MainColor);
		};
	}

	private static void ChangeBackColorAnimation(Control control, Color from, Color to, double time=0.2)
	{
		var colorAnimation = new ColorAnimation
		{
			From = from,
			To = to,
			Duration = new Duration(TimeSpan.FromSeconds(time)),
			EasingFunction = new QuadraticEase()
		};
		control.Background.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
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