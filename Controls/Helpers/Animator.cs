using Microsoft.UI.Xaml.Media.Animation;

namespace FoxyBrowser716_WinUI.Controls.Helpers;

public static class Animator
{
	public static void ChangeColorAnimation(Brush? brush, Color to, double time = 0.2)
	{
		if (brush is null) return;
		
		if (brush is SolidColorBrush solidColorBrush)
		{
			// make them the same color, just one is transparent.
			// avoids the default of starting or ending at a transparent white.
			
			if (solidColorBrush.Color.A == 0)
				solidColorBrush.Color = Color.FromArgb(0, to.R, to.G, to.B);
			if (to.A == 0)
				to = Color.FromArgb(0, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B);
		}
		
		var animation = new ColorAnimation
		{
			To = to,
			Duration = new Duration(TimeSpan.FromSeconds(time)),
			EasingFunction = new QuadraticEase()
		};

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		Storyboard.SetTarget(animation, brush);
		Storyboard.SetTargetProperty(animation, "Color");

		storyboard.Begin();
	}
}