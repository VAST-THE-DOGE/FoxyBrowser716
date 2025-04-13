using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using Point = System.Drawing.Point;

namespace FoxyBrowser716.HomeWidgets;

public partial class RougeWidget : IWidget
{
	public RougeWidget()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "RougeWidget";
	public override string WidgetName => StaticWidgetName;
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings)
	{
		base.Initialize(manager, settings);
		var timer = new System.Timers.Timer(50);
		timer.Elapsed += Tick;
		timer.AutoReset = true;
		timer.Enabled = true;
		
		return Task.CompletedTask;
	}

	private List<RougeLiteParticles.BoxVectorParticle> particles = [];
	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		Dispatcher.Invoke(() =>
		{
			var particle = new RougeLiteParticles.BoxVectorParticle(new Vector2(0,5), new Vector2(2,0), new Point(10,10), 5, Brushes.Aqua);
			particles.Add(particle);
			canvas.Children.Add(particle.Box);

			foreach (var p in particles)
			{
				p.Move();
			}
		});
	}
}