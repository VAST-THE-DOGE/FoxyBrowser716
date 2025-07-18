using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Material.Icons.WPF;

namespace FoxyBrowser716;

public partial class TabCard
{
    public event Action? DuplicateRequested;
    public event Action? RemoveRequested;
    public event Action? CardClicked;

    public int Key { get; private set; }
    private bool _active;

    public event Action<TabCard, int?>? DragPositionChanged;
    private bool _isDragging;
    public Point DragStartPoint { get; private set; }
    
    public TabCard(WebsiteTab tab, bool useDragging = true) : this(tab.TabId, tab, useDragging) {}
    public TabCard(int key, WebsiteTab tab, bool useDragging = true)
    {
        InitializeComponent();
        Key = key;

        TitleLabel.Content = tab.Title;
        TabIcon.Child = new Image {Source = tab.Icon.Source};

        DuplicateButton.Click += (_, _) => DuplicateRequested?.Invoke();
        CloseButton.Click += (_, _) => RemoveRequested?.Invoke();
        
        SetupAnimations();
        
        if (useDragging)
            AttachDragHandlers();
        else
            MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();
    }

    public TabCard(int key, TabInfo tab, bool useDragging = false)
    {
        InitializeComponent();
        Key = key;

        DuplicateButton.Visibility = Visibility.Collapsed;
        TitleLabel.Content = tab.Title;

        TabIcon.Child = tab.Image;

        CloseButton.Click += (_, _) => RemoveRequested?.Invoke();
        
        SetupAnimations();
        
        if (useDragging)
            AttachDragHandlers();
        else
            MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();
    }
    
    public TabCard(BitmapSource? bitmapSource, string name)
    {
        InitializeComponent();

        DuplicateButton.Visibility = Visibility.Collapsed;
        CloseButton.Visibility = Visibility.Collapsed;

        TitleLabel.Content = name;
        if (bitmapSource is not null)
            TabIcon.Child = new Image {Source = bitmapSource, Stretch = Stretch.Uniform};
        else
            TabIcon.Visibility = Visibility.Hidden;
        
        SetupAnimations();
        
        MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();
    }

    public TabCard(MaterialIcon? icon, string name)
    {
        InitializeComponent();

        DuplicateButton.Visibility = Visibility.Collapsed;
        CloseButton.Visibility = Visibility.Collapsed;

        TitleLabel.Content = name;
        if (icon is not null)
            TabIcon.Child = icon;
        else
            TabIcon.Visibility = Visibility.Hidden;
        
        SetupAnimations();
        
        MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();
    }

    private void SetupAnimations()
    {
        RootBorder.Background = new SolidColorBrush(_transparentBack);
        RootBorder.BorderBrush = new SolidColorBrush(_transparentAccent);
        
        CloseButton.MouseEnter += (_, _) =>
            { ChangeColorAnimation(CloseButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.Red); };
        CloseButton.MouseLeave += (_, _) =>
            { ChangeColorAnimation(CloseButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.White); };
        
        DuplicateButton.MouseEnter += (_, _) =>
            { ChangeColorAnimation(DuplicateButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.DodgerBlue); };
        DuplicateButton.MouseLeave += (_, _) =>
            { ChangeColorAnimation(DuplicateButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.White); };

        RootPanel.MouseEnter += (_, _) => { ChangeColorAnimation(RootBorder.Background, ((SolidColorBrush)RootBorder.Background).Color, _transparentAccent); };
        RootPanel.MouseLeave += (_, _) => { ChangeColorAnimation(RootBorder.Background, ((SolidColorBrush)RootBorder.Background).Color, _transparentBack); };
    }
        
    private readonly Color _transparentBack = Color.FromArgb(225, 48, 50, 58);
    private readonly Color _transparentAccent = Color.FromArgb(255, AccentColor.R, AccentColor.G, AccentColor.B);
    private readonly Color _transparentHighlight = Color.FromArgb(255, HighlightColor.R, HighlightColor.G, HighlightColor.B);
    public void ToggleActiveTo(bool active)
    {
        _active = active;

        ChangeColorAnimation(RootBorder.BorderBrush, 
            ((SolidColorBrush)RootBorder.BorderBrush).Color, 
            _active ? _transparentHighlight : MainColor,
            _active ? 0.25 : 0.5);
    }
    
    private void AttachDragHandlers()
    {
        MouseLeftButtonUp += TabCard_MouseLeftButtonUp;
        MouseLeftButtonDown += TabCard_MouseLeftButtonDown;
        MouseMove += TabCard_MouseMove;
    }
    
    private void TabCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragStartPoint = e.GetPosition(null);
        CaptureMouse();
    }

    private void TabCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || !IsMouseCaptured)
        {
            ReleaseMouseCapture();
            _isDragging = false;
            Opacity = 1.0;
            return;
        }
        
        if (Parent is not StackPanel stackPanel) return;
        var currentPosition = e.GetPosition(stackPanel);
        var diff = e.GetPosition(null) - DragStartPoint;

        if (!_isDragging && diff.Length > SystemParameters.MinimumHorizontalDragDistance)
        {
            _isDragging = true;
            Opacity = 0.5;
        }

        if (_isDragging)
        {
            var relativeMove = CalculateRelativeMove(currentPosition);
            DragPositionChanged?.Invoke(this, relativeMove);
        }
    }

    private void TabCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            ReleaseMouseCapture();
            _isDragging = false;
            Opacity = 1.0;
        }
        else
        {
            CardClicked?.Invoke();
        }
    }

    private int? CalculateRelativeMove(Point currentPosition)
    {
        if (currentPosition.X < 0 || currentPosition.X > Width) return null;
        
        var move = (int)(currentPosition.Y / 25);
        return move == 0 ? (int?)0 : move;
    }
}