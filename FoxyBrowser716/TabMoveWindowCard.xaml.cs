using System.Windows;
using System.Windows.Input;

namespace FoxyBrowser716;

public partial class TabMoveWindowCard : Window
{
	private WebsiteTab Tab;
	private ServerManager Instance;
	private Point GrabLocation;
    
	public TabMoveWindowCard(ServerManager instance, WebsiteTab tab, Point grabLocation)
	{
		InitializeComponent();

		Instance = instance;
		Tab = tab;
		
		GrabLocation = grabLocation;

		TitleLabel.Content = Tab.Title;
		TabIcon.Child = Tab.Icon;
		
		Loaded += (s, e) => CaptureMouse();
		PreviewMouseMove += OnMouseMove;
		PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
	}

	//private Point? gl;
	private void OnMouseMove(object sender, MouseEventArgs e)
	{
		var mousePos = Mouse.GetPosition(null);
		mousePos = new Point(mousePos.X - GrabLocation.X, mousePos.Y - GrabLocation.Y);
		var screenPos = PointToScreen(mousePos);

		Left = screenPos.X;
		Top = screenPos.Y;
	}

	private async void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
	{
		var mousePos = Mouse.GetPosition(null);
		var screenPos = PointToScreen(mousePos);
		
		ReleaseMouseCapture();
		await Instance.CreateWindowFromTab(Tab, screenPos);
		Close();
	}
}