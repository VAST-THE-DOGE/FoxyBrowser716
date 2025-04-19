using System.Numerics;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using Point = System.Drawing.Point;

namespace FoxyBrowser716.HomeWidgets;

public partial class LAGWIDGET : IWidget
{
	public LAGWIDGET()
	{
		InitializeComponent();
	}

	public const string StaticWidgetName = "LagWidget (Warning)";
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

	private List<RoguelikeParticleClasses.BoxVectorParticle> particles = [];
	private Brush particleBrush = new SolidColorBrush(Colors.White);
	private Random random = new();
	private float collisionMultiplier = 1.1f;
	private void RandomizeColor()
	{
		particleBrush = new SolidColorBrush(Color.FromArgb((byte)random.Next(0,250),(byte)random.Next(0,255),(byte)random.Next(0,255),(byte)random.Next(0,255)));
	}
	private void Tick(object? sender, ElapsedEventArgs elapsedEventArgs)
	{
		var t = DebugHelpers.StartDebugTimer();

		var Randomized = false;
		Dispatcher.Invoke(() =>
		{
			for (var i = 0; i < 5; i++)
			{
				var particle = new RoguelikeParticleClasses.BoxVectorParticle(
					new Vector2(random.Next(-30, 30), random.Next(-30, 30)), new Vector2(0, 0), new Point(10, 10), random.Next(5, 30),
					particleBrush);
				particles.Add(particle);
				canvas.Children.Add(particle.Box);
			}

			foreach (var p in particles.ToList())			
			{
				if (p.Velocity.X > 100 || p.Velocity.X < -100 || p.Velocity.Y < -100 ||
				    p.Velocity.Y > 100)
				{
					particles.Remove(p);
					canvas.Children.Remove(p.Box);
				}
				
				p.Move();
				if (p.Box.X1 < 0)
				{
					p.Box.X1 = 0;
					p.Box.X2 = 0;
					p.Velocity.X *= -collisionMultiplier;
					p.Velocity.Y *= collisionMultiplier;
					if (!Randomized)
					{
						RandomizeColor();
						Randomized = true;
					}
				}
				else if (p.Box.X1 > ActualWidth)
				{
					p.Box.X1 = ActualWidth;
					p.Box.X2 = ActualWidth;
					p.Velocity.X *= -collisionMultiplier;
					p.Velocity.Y *= collisionMultiplier;
					if (!Randomized)
					{
						RandomizeColor();
						Randomized = true;
					}
				}
				if (p.Box.Y1 < 0)
				{
					p.Box.Y1 = 0;
					p.Box.Y2 = p.ParticleSize;
					p.Velocity.Y *= -collisionMultiplier;
					p.Velocity.X *= collisionMultiplier;
					if (!Randomized)
					{
						RandomizeColor();
						Randomized = true;
					}
				}
				else if (p.Box.Y1 > ActualHeight)
				{
					p.Box.Y1 = ActualHeight - p.ParticleSize;
					p.Box.Y2 = ActualHeight;
					p.Velocity.Y *= -collisionMultiplier;
					p.Velocity.X *= collisionMultiplier;
					if (!Randomized)
					{
						RandomizeColor();
						Randomized = true;
					}
				}
			}
		});
		
		t.EndDebugTimer();
	}
}