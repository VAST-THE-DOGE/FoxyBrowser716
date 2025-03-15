using static FoxyBrowser716.ColorPalette;
using static FoxyBrowser716.Animator;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
    private Point _dragStartPoint;
    
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

    private void SetupAnimations()
    {
        RootPanel.Background = new SolidColorBrush(MainColor);

        CloseButton.MouseEnter += (_, _) =>
            { ChangeColorAnimation(CloseButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.Red); };
        CloseButton.MouseLeave += (_, _) =>
            { ChangeColorAnimation(CloseButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.White); };

        DuplicateButton.MouseEnter += (_, _) =>
            { ChangeColorAnimation(DuplicateButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.DodgerBlue); };
        DuplicateButton.MouseLeave += (_, _) =>
            { ChangeColorAnimation(DuplicateButton.Foreground, ((SolidColorBrush)RootPanel.Background).Color, Colors.White); };

        RootPanel.MouseEnter += (_, _) =>
            { if (!_active) ChangeColorAnimation(RootPanel.Background, ((SolidColorBrush)RootPanel.Background).Color, AccentColor); };
        RootPanel.MouseLeave += (_, _) =>
            { if (!_active) ChangeColorAnimation(RootPanel.Background, ((SolidColorBrush)RootPanel.Background).Color, MainColor); };
    }
        
    public void ToggleActiveTo(bool active)
    {
        _active = active;

        ChangeColorAnimation(RootPanel.Background, 
            ((SolidColorBrush)RootPanel.Background).Color, 
            _active ? HighlightColor : MainColor,
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
        _dragStartPoint = e.GetPosition(null);
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
        };
        
        var currentPosition = e.GetPosition(null);
        var diff = currentPosition - _dragStartPoint;

        if (!_isDragging && diff.Length > SystemParameters.MinimumHorizontalDragDistance)
        {
            _isDragging = true;
            Opacity = 0.5;
        }

        if (_isDragging)
        {
            int? relativeMove = CalculateRelativeMove(currentPosition);
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
        
        var move = (int)((currentPosition.Y - _dragStartPoint.Y) / 30);
        return move == 0 ? (int?)0 : move;
    }

    public void STOP() //TODO: not needed?
    {
        _isDragging = false;
        _active = false;
        Visibility = Visibility.Collapsed;
    }
}