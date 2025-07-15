using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;

namespace FoxyBrowser716.HomeWidgets;

public partial class MediaPlayerWidget : Widget
{
	public MediaPlayerWidget()
	{
		InitializeComponent();
	}
	
	public const string StaticWidgetName = "MediaWidget";
	public override string WidgetName => StaticWidgetName;

	public override Dictionary<string, IWidgetSetting>? WidgetSettings { get; set; } =
		new()
		{
			["FolderQuickAccessCount"]=new WidgetSettingInt(5),
		};

	private void Blur_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		var geometry = new RectangleGeometry(
			new Rect(0, 0, BlurredBackground.ActualWidth, BlurredBackground.ActualHeight),
			8, 8); // RadiusX and RadiusY set to 8 (inner corner radius)
		BlurredBackground.Clip = geometry;
	}
	
	public override Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings = null)
	{
		base.Initialize(manager, settings);
		
		BlurredBackground.Effect = new BlurEffect { Radius = 25 };
		BlurredBackground.Opacity = 0.3;
		var visualBrush = new VisualBrush(MediaIcon)
		{
			Stretch = Stretch.UniformToFill,
			TileMode = TileMode.None,
			Transform = new ScaleTransform(1.3, 1.3, 0.5, 0.5)
		};
		BlurredBackground.Background = visualBrush;
		
		var hoverColor = Color.FromArgb(50,255,255,255);
		foreach (var b in (Button[])
		         [
			         ButtonFolderDropDown
		         ])
		{
			b.MouseEnter += (_, _) => { ChangeColorAnimation(b.Background, Colors.Transparent, hoverColor); };
			b.MouseLeave += (_, _) => { ChangeColorAnimation(b.Background, hoverColor, Colors.Transparent); };
			
			b.PreviewMouseUp += (_, _) => { ChangeColorAnimation(b.Foreground, HighlightColor, Colors.White); };
			b.PreviewMouseDown += (_, _) => { ChangeColorAnimation(b.Foreground, Colors.White, HighlightColor); };
		}
		
		return Task.CompletedTask;
	}
}