using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace FoxyBrowser716;

public partial class FoxyPopup : Window
{
	private string _title;
	public string Title
	{
		get => _title;
		set
		{
			_title = value;
			LabelTitle.Content = value;
		}
	}
	
	private string _subtitle;
	public string Subtitle
	{
		get => _subtitle;
		set
		{
			_subtitle = value;
			LabelSubtitle.Text = value;
		}
	}
	
	private bool _showProgressbar;
	public bool ShowProgressbar
	{
		get => _showProgressbar;
		set
		{
			_showProgressbar = value;
			ProgressBar.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	public FoxyPopup()
	{
		InitializeComponent();
		MouseLeftButtonDown += (_, e) =>
		{
			if (e.ButtonState == MouseButtonState.Pressed)
			{
				DragMove();
			}
		};
		
		SetupProgressBarAnimation();
	}
	
	private void SetupProgressBarAnimation()
	{
		var animation1 = new DoubleAnimation
		{
			From = 0.0,
			To = 1.2,
			Duration = TimeSpan.FromSeconds(3),
			RepeatBehavior = RepeatBehavior.Forever,
			//AutoReverse = true,
			EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseInOut, Exponent = 2},

		};
        
		var animation2 = new DoubleAnimation
		{
			From = -0.4,
			To = 1.8,
			Duration = TimeSpan.FromSeconds(3),
			RepeatBehavior = RepeatBehavior.Forever,
			//AutoReverse = true,
			EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseInOut, Exponent = 2},
		};
        
		var animation3 = new DoubleAnimation
		{
			From = -0.8,
			To = 2.4,
			Duration = TimeSpan.FromSeconds(3),
			RepeatBehavior = RepeatBehavior.Forever,
			//AutoReverse = true,
			EasingFunction = new ExponentialEase {EasingMode = EasingMode.EaseInOut, Exponent = 2},
		};
        
		Stop1.BeginAnimation(GradientStop.OffsetProperty, animation1);
		Stop2.BeginAnimation(GradientStop.OffsetProperty, animation2);
		Stop3.BeginAnimation(GradientStop.OffsetProperty, animation3);
	}


	public void SetButtons(params BottomButton[] buttons)
	{
		foreach (var b in buttons)
		{
			var button = new Button()
			{
				BorderBrush = new SolidColorBrush(Styling.ColorPalette.HighlightColor),
				Background = new SolidColorBrush(Styling.ColorPalette.AccentColor),
				Foreground = b.TextColor is null ? Brushes.White : new SolidColorBrush(b.TextColor.Value),
				Margin = new Thickness(10,3,10,3),
				FontSize = 14,
				FontWeight = FontWeights.SemiBold,
				Content = b.Text,
			};
			button.Click += (_,_) => b.OnClick?.Invoke();
			
			ButtonStack.Children.Add(button);
		}
	}

	public class BottomButton(Action onClick, string text, Color? textColor = null)
	{
		public Action? OnClick { get; set; } = onClick;
		public string Text { get; set; } = text;
		public Color? TextColor { get; set; } = textColor;
	};
}