using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static FoxyBrowser716.Styling.Animator;

namespace FoxyBrowser716;

public partial class SearchEnginePicker : Window
{
	public event Action<InfoGetter.SearchEngine>? OnSearchEnginePicked;
	
	public SearchEnginePicker(InfoGetter.SearchEngine curEng)
	{
		InitializeComponent();
		
		var engAvl = Enum
			.GetValues<InfoGetter.SearchEngine>()
			.Where(e => e != curEng);
		
		FocusableChanged += (_, _) =>
		{
			if (!Focusable) return;
			Focus();
		};
		
		var finalHeight = 4;
		
		var hoverColor = Color.FromArgb(50,255,255,255);
		
		foreach (var e in engAvl)
		{
			finalHeight += 20;
			var b = new Button()
			{
				Content = new Image()
				{
					Source = new BitmapImage(new Uri(InfoGetter.GetSearchEngineIcon(e))),
					Width = 14, Height = 14,
					Stretch = Stretch.Uniform,
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Center,
				},
				Background = new SolidColorBrush(Colors.Transparent),
				Foreground = new SolidColorBrush(Colors.Transparent),
				BorderBrush = new SolidColorBrush(Colors.Transparent),
				Width = 20, Height = 20,
				Style = (Style)FindResource("CircularButtonStyle"),
			};
			
			b.MouseEnter += (_, _) => { ChangeColorAnimation(b.Background, Colors.Transparent, hoverColor); };
			b.MouseLeave += (_, _) => { ChangeColorAnimation(b.Background, hoverColor, Colors.Transparent); };

			b.Click += (_, _) =>
			{
				OnSearchEnginePicked?.Invoke(e);
				Close();
			};
			
			Height = finalHeight;
			
			StackStack.Children.Add(b);
		}
	}
}