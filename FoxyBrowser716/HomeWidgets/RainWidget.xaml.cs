using System.Diagnostics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
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

	public override Dictionary<string, IWidgetSetting>? WidgetSettings { get; set; } = new()
	{
		["RainDropMultiplier"] = new WidgetSettingDouble(1),
	};
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		var timer = new System.Timers.Timer(25);
		timer.Elapsed += Tick;
		timer.AutoReset = true;
		timer.Enabled = true;
		
		return Task.CompletedTask;
	}

	// used to slow down FPS on low-end systems
	private bool inTick;
	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			// shouldn't run if there is no one to see it
			if (inTick || !IsVisible) return;
			inTick = true;
			
			if (WidgetSettings?["RainDropMultiplier"] is not WidgetSettingDouble settingDouble) return;

			// used to prevent grouping
			// when moving a window (decreasing height), a lot of raindrops go to the top in a single wave without this.
			var movedThisTick = 0;
			
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
					else if (_shapes.Count < settingDouble.Value * (ActualHeight) / 4)
					{
						if (movedThisTick++ >= 5) continue;
						
						var position = _random.Next(-((int)ActualHeight / 4), (int)ActualWidth);
						line.X1 = position;
						line.X2 = position + 2;
						line.Y1 = 0;
						line.Y2 = 10;
						if (line.Parent == canvas && line.X1 < 0)
							canvas.Children.Remove(line);
					}
					else
					{
						_shapes.Remove(line);
						if (line.Parent == canvas)
							canvas.Children.Remove(line);
					}
				}
			}
			
			if (_shapes.Count < settingDouble.Value * (ActualHeight)/5)
			{
				for (var i = 0; i < 3; i++)
				{
					var position = _random.Next(-((int)ActualHeight / 4), (int)ActualWidth);
					var newLine = new Line
					{
						X1 = position, X2 = position + 2, Y1 = 0, Y2 = 10,
						Stroke = Brushes.CornflowerBlue,
						StrokeThickness = 1
					};
					_shapes.Add(newLine);
				}
			}
			
			inTick = false;
		});
	}
}