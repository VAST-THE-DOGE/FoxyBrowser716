using System.ComponentModel;
using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class RougelikeWidget : IWidget
{
	public RougelikeWidget()
	{
		InitializeComponent();
		canvas.DataContext = this;
	}

	#region Texures
	// internal Image
	public Image MoneyImage = new() 
	{
		Stretch = Stretch.Uniform,
		Source = new BitmapImage(new Uri("C:\\Users\\penfo\\RiderProjects\\FoxyBrowser716\\FoxyBrowser716\\RoguelikeData\\CardIcons\\FoxyCoin.png", UriKind.RelativeOrAbsolute))
	};
	public string MoneyAmount = "12345";
	#endregion
	
	public const string StaticWidgetName = "RougelikeWidget";
	public override string WidgetName => StaticWidgetName;
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		Loaded += RougeWidgetLoaded;
		
		base.Initialize(manager, settings);
		
		var timer = new System.Timers.Timer(30);
		timer.Elapsed += Tick;
		timer.AutoReset = true;
		timer.Enabled = true;

		playerBox.Stroke = Brushes.Orange;
		playerBox.StrokeThickness = playerWidth;
		canvas.Children.Add(playerBox);
		
		return Task.CompletedTask;
	}

	private Grid ParentGrid { get; set; }
	private Viewbox viewbox;
	private void RougeWidgetLoaded(object sender, RoutedEventArgs e)
	{
		var parent = VisualTreeHelper.GetParent(this);
		while (parent != null && !(parent is Grid))
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		ParentGrid = parent as Grid;
	}

	private void StartButtonClicked(object sender, RoutedEventArgs e)
	{
		if (ParentGrid == null)
		{
			MessageBox.Show("ParentGrid not set!");
			return;
		}

		if (viewbox == null)
		{
			canvas.Visibility = Visibility.Visible;
			widgetGrid.Children.Remove(canvas);

			viewbox = new Viewbox
			{
				Stretch = Stretch.UniformToFill,
				Child = canvas,
				IsHitTestVisible = false
			};

			ParentGrid.Children.Add(viewbox);
			Grid.SetRowSpan(viewbox, ParentGrid.RowDefinitions.Count > 0 ? ParentGrid.RowDefinitions.Count : 1);
			Grid.SetColumnSpan(viewbox, ParentGrid.ColumnDefinitions.Count > 0 ? ParentGrid.ColumnDefinitions.Count : 1);
			Panel.SetZIndex(viewbox, 100);

			ParentGrid.PreviewKeyDown += CanvasKeyDown;
			ParentGrid.PreviewKeyUp += CanvasKeyUp;
		}
		else
		{
			DeactivateGame();
		}
	}
	
	private void DeactivateGame()
	{
		if (viewbox != null && ParentGrid != null)
		{
			ParentGrid.Children.Remove(viewbox);

			widgetGrid.Children.Add(canvas);
			canvas.Visibility = Visibility.Collapsed;

			viewbox = null;
		}
	}
	
	private RoguelikePlayer player = new ();
	private const int playerWidth = 50;
	private const int playerHeight = 100;

	private void CanvasKeyDown(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.A or Key.D:
				player.lastLeftRight = e.Key;
				break;
			case Key.S or Key.W:
				player.lastUpDown = e.Key;
				break;
		}
	}
	
	private void CanvasKeyUp(object sender, KeyEventArgs e)
	{
		switch (e.Key)
		{
			case Key.A:
				player.lastLeftRight = Keyboard.IsKeyDown(Key.D) ? Key.D : null;
				break;
			case Key.D:
				player.lastLeftRight = Keyboard.IsKeyDown(Key.A) ? Key.A : null;
				break;
			case Key.S:
				player.lastUpDown = Keyboard.IsKeyDown(Key.W) ? Key.W : null;
				break;
			case Key.W:
				player.lastUpDown = Keyboard.IsKeyDown(Key.S) ? Key.S : null;
				break;
		}
	}

	private Line playerBox = new()
	{
		X1 = 0, X2 = 0, Y1 = 0, Y2 = 0,
		Stroke = Brushes.Orange,
		StrokeThickness = playerWidth
	};
	
	private int tick;
	private double MovementMult = 1;
	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		var debugTimer = DebugHelpers.StartDebugTimer();
		tick++;
		
		var speed = GetMovementSpeed();
		
		Dispatcher.Invoke(() => DoPlayerMovement(speed));
		
		if (tick % 10 == 0)
		{
			UpdateMovementMult();
		}
		
		debugTimer.EndDebugTimer();
	}
	
	private bool inTick;
	private void DoPlayerMovement(double speed)
	{
		if (inTick || !canvas.IsVisible) return;
		
		player.left = player switch
		{
			{ lastLeftRight: Key.A, left: > 0 } => double.Max(0, player.left - speed),
			{ lastLeftRight: Key.D } when player.left < canvas.Width - playerWidth => 
				double.Min(player.left + speed, (int)canvas.Width - playerWidth),
			_ => player.left
		};
		player.top = player switch
		{
			{ lastUpDown: Key.W, top: > 0 } => double.Max(0, player.top - speed),
			{ lastUpDown: Key.S } when player.top < canvas.Height - playerHeight * 2 => 
				double.Min(player.top + speed, (int)canvas.Height - playerHeight * 2),
			_ => player.top
		};
			
		playerBox.X1 = player.left + playerWidth/2d;
		playerBox.Y1 = player.top;
		playerBox.X2 = player.left + playerWidth/2d;
		playerBox.Y2 = player.top + playerHeight;
	}

	private void UpdateMovementMult()
	{
		MovementMult = 1;
	}

	private double GetMovementSpeed()
	{
		return 3 * MovementMult;
	}
}