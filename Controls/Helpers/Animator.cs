using Microsoft.UI.Xaml.Media.Animation;

namespace FoxyBrowser716_WinUI.Controls.Helpers;

public static class Animator
{
	public static void ChangeColorAnimation(Brush? brush, Color to, double time = 0.2)
	{
		if (brush is null) return;
		
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