using System.IO.Pipes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		
		var mutex = new Mutex(true, "FoxyBrowser716_Mutex", out var isNewInstance);
		if (!isNewInstance)
		{
			SendMessage($"NewWindow|{e.Args.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))}");
			return;
		}
		
		ServerManager.RunServer(e);
		mutex.ReleaseMutex();
	}
    
	static void SendMessage(string message)
	{
		using var client = new NamedPipeClientStream(".", "FoxyBrowser716_Pipe", PipeDirection.Out);
		try
		{
			client.Connect(1000);
			using var writer = new System.IO.StreamWriter(client);
			writer.WriteLine(message);
			writer.Flush();
		}
		catch
		{
			// Handle if connection fails
		}
	}
}






//TODO: this shouldn't be here:
public static class ColorPalette
{
	public static Color MainColor => Color.FromRgb(30, 35, 50);
	public static Color AccentColor => Color.FromRgb(54, 64, 91);

	public static Color HighlightColor => Color.FromRgb(255, 145, 3);
}

public static class Animator
{
	/// <summary>
	/// Changes the color of a brush to do cool animations!
	/// </summary>
	/// <param name="brush">The brush that is animated such as "Control.Background." NOTE: The brush has to be custom (i.e. using new brush or specifying the color as hex '#000000' in xml)</param>
	/// <param name="from">The color that the animation start from</param>
	/// <param name="to">The color that the animation ends on (final color)</param>
	/// <param name="time">Time for the animation to run in seconds</param>
	public static void ChangeColorAnimation(Brush brush, Color from, Color to, double time = 0.2)
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

	
}