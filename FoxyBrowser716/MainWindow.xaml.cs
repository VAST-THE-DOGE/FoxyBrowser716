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

	private Rectangle _originalRectangle;
	
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
			button.PreviewMouseLeftButtonDown += (_, _) =>
			{
				button.Foreground = new SolidColorBrush(HighlightColor);
			};
			button.PreviewMouseLeftButtonUp += (_, _) =>
			{
				ChangeColorAnimation(button.Foreground, HighlightColor, Colors.White);
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
		this.Left = 0;
		this.Top = 0;
		this.Width = SystemParameters.PrimaryScreenWidth;
		this.Height = SystemParameters.PrimaryScreenHeight;
	}

	private void Close_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
	
	#region ResizeWindows
	bool ResizeInProcess = false;
	private void Resize_Init(object sender, MouseButtonEventArgs e)
	{
		Rectangle senderRect = sender as Rectangle;
		if (senderRect != null)
		{
			ResizeInProcess = true;
			senderRect.CaptureMouse();
		}
	}

	private void Resize_End(object sender, MouseButtonEventArgs e)
	{
		Rectangle senderRect = sender as Rectangle;
		if (senderRect != null)
		{
			ResizeInProcess = false; ;
			senderRect.ReleaseMouseCapture();
		}
	}

	private void Resizeing_Form(object sender, MouseEventArgs e)
	{
		if (ResizeInProcess)
		{
			Rectangle senderRect = sender as Rectangle;
			Window mainWindow = senderRect.Tag as Window;
			if (senderRect != null)
			{
				double width = e.GetPosition(mainWindow).X;
				double height = e.GetPosition(mainWindow).Y;
				senderRect.CaptureMouse();
				if (senderRect.Name.ToLower().Contains("right"))
				{
					width += 1;
					if (width > 0)
						mainWindow.Width = width;
				}
				if (senderRect.Name.ToLower().Contains("left"))
				{
					width -= 1;
					mainWindow.Left += width;
					width = mainWindow.Width - width;
					if (width > 0)
					{
						mainWindow.Width = width;
					}
				}
				if (senderRect.Name.ToLower().Contains("bottom"))
				{
					height += 1;
					if (height > 0)
						mainWindow.Height = height;
				}
				if (senderRect.Name.ToLower().Contains("top"))
				{
					height -= 1;
					mainWindow.Top += height;
					height = mainWindow.Height - height;
					if (height > 0)
					{
						mainWindow.Height = height;
					}
				}
			}
		}
	}
	#endregion
}