using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FoxyBrowser716.Styling;

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
