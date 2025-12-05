using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716.Controls.Generic;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716.Controls.MainWindow;

[ObservableObject]
public sealed partial class NewTabCard : UserControl
{
	[ObservableProperty] public partial bool ForceHighlight { get; set; }
	[ObservableProperty] public partial string Title { get; set; }
	
	[ObservableProperty] public partial UIElement Icon { get; set; }
	
	[ObservableProperty] public partial Visibility CloseVisible { get; set; } = Visibility.Visible;
	[ObservableProperty] public partial Visibility DuplicateVisible { get; set; } = Visibility.Visible;

	[ObservableProperty] 
	public partial ObservableCollection<FMenuItem> MenuOptions { get; set; } = 
		[ 
			new() { Text = "test1", Action = () => Debug.WriteLine("hey1"), }, 
			new() { Text = "test2", Action = () => Debug.WriteLine("hey2") }, 
			new() { Text = "test3", Action = () => Debug.WriteLine("hey3") }, 
			new() { Text = "test4", Action = () => Debug.WriteLine("hey4") },
		];
	
	public event Action<NewTabCard>? DuplicateRequested;
	public event Action<NewTabCard>? CloseRequested;
	public event Action<NewTabCard>? OnClick;
	
	public event Action<NewTabCard, ManipulationStartedRoutedEventArgs>? OnDragStarted;
	public event Action<NewTabCard, ManipulationDeltaRoutedEventArgs>? OnDragMoved;
	public event Action<NewTabCard, ManipulationCompletedRoutedEventArgs>? OnDragCompleted;
	
	public static readonly DependencyProperty CurrentThemeProperty = DependencyProperty.Register(
		nameof(CurrentTheme),
		typeof(Theme),
		typeof(NewTabCard),
		new PropertyMetadata(DefaultThemes.DarkMode, (d, e) => ((NewTabCard)d).OnCurrentThemeChanged((Theme)e.NewValue)));

	public Theme CurrentTheme
	{
		get => (Theme)GetValue(CurrentThemeProperty);
		set => SetValue(CurrentThemeProperty, value);
	}
	
	private void OnCurrentThemeChanged(Theme newTheme)
	{
		ApplyTheme();
	}
	
	public NewTabCard()
	{
		InitializeComponent();
	}
	
	private SolidColorBrush GetBorderBrush(Theme theme, bool forceHighlight) => new(forceHighlight ? theme.PrimaryHighlightColor : theme.SecondaryBackgroundColor); 

	private void ApplyTheme()
	{
		// Root.BorderBrush = new SolidColorBrush(ForceHighlight ? CurrentTheme.PrimaryHighlightColor : CurrentTheme.SecondaryBackgroundColor);
		Root.Background = new SolidColorBrush(_mouseOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : CurrentTheme.PrimaryBackgroundColorVeryTransparent);

		if (Icon is FrameworkElement iconElement)
		{
			iconElement.SetValue(ForegroundProperty, new SolidColorBrush(CurrentTheme.PrimaryForegroundColor));
		}

		ButtonClose.CurrentTheme = CurrentTheme with
		{
			SecondaryForegroundColor = CurrentTheme.NoColor,
			PrimaryHighlightColor = CurrentTheme.NoColor
		};
		ButtonDuplicate.CurrentTheme = CurrentTheme;
	}
	
	private void ButtonClose_OnOnClick(object sender, RoutedEventArgs e)
	{
		CloseRequested?.Invoke(this);
	}

	private void ButtonDuplicate_OnOnClick(object sender, RoutedEventArgs e)
	{
		DuplicateRequested?.Invoke(this);
	}
	
	#region ColorAnimators
	private bool _mouseOver;
	private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
	{
		_mouseOver = true;
		ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
	}

	private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
	{
		_mouseOver = false;
		ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent);
	}
	
	private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;
		if (ButtonDuplicate.PointerOver || ButtonClose.PointerOver) return;
        
		ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent, 0.3);
		OnClick?.Invoke(this);
	}
	
	private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
		if (ButtonDuplicate.PointerOver || ButtonClose.PointerOver) return;

		ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryHighlightColorSlightTransparent, 0.05);
	}
	#endregion

	private void Root_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	{
		if (ButtonDuplicate.PointerOver || ButtonClose.PointerOver)
		{
			e.Complete();
			return;
		}
		
		OnDragStarted?.Invoke(this, e);
	}

	private void Root_OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		OnDragCompleted?.Invoke(this, e);
	}

	private void Root_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	{
		OnDragMoved?.Invoke(this, e);
	}
}