using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets;

public partial class RainWidget : IWidget
{
	public RainWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "RainWidget";
	public override string WidgetName => StaticWidgetName;

	private List<Shape> _shapes = [];
	private Random _random = new();
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		var timer = new System.Timers.Timer(30);
		timer.Elapsed += Tick;
		timer.AutoReset = true;
		timer.Enabled = true;
		
		return Task.CompletedTask;
	}

	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			Console.WriteLine("s: "+_shapes.Count);
			Console.WriteLine("c: "+canvas.Children.Count);
			foreach (var shape in _shapes.ToList())
			{
				if (shape is Line line)
				{
					if (line.Parent is null && line.X2 > 2)
					{
						line.X1 = line.X2 - 2;
						line.Y2 = line.Y1 + 10;
						canvas.Children.Add(line);
					}
					else if (line.Y1 < ActualHeight)
					{
						line.Y1 += 20;
						line.Y2 += 22;
						line.X1 += 4;
						line.X2 += 4.4;
					} 
					else
					{
						_shapes.Remove(line);
						if (line.Parent == canvas)
							canvas.Children.Remove(line);
					}
				}
			}

			for (var i = 0; i < 3; i++)
			{
				var position = _random.Next(-((int)ActualHeight/4), (int)ActualWidth);
				var newLine = new Line
				{
					X1 = position, X2 = position + 2, Y1 = 0, Y2 = 10,
					Stroke = Brushes.CornflowerBlue,
					StrokeThickness = 1
				};
				_shapes.Add(newLine);
			}
		});
	}
}