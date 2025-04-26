using System.IO.Pipes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FoxyBrowser716;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		
		var mutex = new Mutex(true, "FoxyBrowser716_Mutex", out var isNewInstance);
		if (!isNewInstance)
		{
			SendMessage($"NewWindow|{e.Args.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))}");
			return;
		}
		
		ServerManager.RunServer(e);
		mutex.ReleaseMutex();
	}
	
	private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		MessageBox.Show($"An unhandled UI exception occurred:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
			"Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
		
		e.Handled = true;
	}

	private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		var exception = e.ExceptionObject as Exception;
		MessageBox.Show($"A fatal error occurred:\n\n{exception?.Message}\n\n{exception?.StackTrace}",
			"Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
	{
		MessageBox.Show($"An unobserved task exception occurred:\n\n{e.Exception.Message}\n\n{e.Exception.InnerException?.Message}",
			"Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
		
		e.SetObserved();
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
		catch (Exception e)
		{
			throw;
		}
	}
}






//TODO: this shouldn't be here:
public static class ColorPalette
{
	public static Color Transparent => Color.FromArgb(0, 0, 0,0);
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