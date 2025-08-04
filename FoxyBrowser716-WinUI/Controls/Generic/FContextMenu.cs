using FoxyBrowser716_WinUI.DataObjects.Basic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FoxyBrowser716_WinUI.Controls.Generic;

public sealed partial class FContextMenu : UserControl
{
    private StackPanel _stackPanel;
    private double _menuWidth;
    private bool _isFocused;
    private List<MenuItem> _currentItems = new();

    private Theme _currentTheme = DefaultThemes.DarkMode;

    internal Theme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            _currentTheme = value;
            ApplyTheme();
        }
    }

    public FContextMenu()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;
    }

    private void InitializeComponent()
    {
        VerticalAlignment = VerticalAlignment.Top;
        HorizontalAlignment = HorizontalAlignment.Left;
        
        _stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = _menuWidth > 4 ? _menuWidth : 4,
        };

        Content = _stackPanel;
        ApplyTheme();
        
        GotFocus += (_, _) => _isFocused = true;
        LostFocus += (_, _) => 
        {
            _isFocused = false;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (!_isFocused)
                {
                    CloseMenu();
                }
            };
            timer.Start();
        };
    }

    private void ApplyTheme()
    {
        _stackPanel.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        _stackPanel.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);

        if (_stackPanel?.Children is not null)
            foreach (var child in _stackPanel.Children)
            {
                if (child is FIconButton iconButton)
                {
                    iconButton.CurrentTheme = CurrentTheme;
                }
                else if (child is FTextButton textButton)
                {
                    textButton.CurrentTheme = CurrentTheme;
                }
            }
    }

    public void SetItems(IEnumerable<MenuItem> items, double maxWidth = 30)
    {
        var itemsA = items.ToArray();
        
        Width = maxWidth;
        _menuWidth = maxWidth;
        _stackPanel.Width = maxWidth;
            
        _stackPanel.Children.Clear();
        _currentItems.Clear();

        if (itemsA.Length == 0)
        {
            CloseMenu();
            return;
        }

        _currentItems.AddRange(itemsA);

        var allIconOnly = itemsA.All(item => item.IsIconOnly);

        if (allIconOnly)
        {
            CreateIconOnlyLayout(itemsA);
        }
        else
        {
            CreateMixedLayout(itemsA);
        }
        
        Visibility = Visibility.Visible;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            Focus(FocusState.Programmatic);
            _isFocused = true;
        };
        timer.Start();
    }

    private void CreateIconOnlyLayout(MenuItem[] items)
    {
        var buttonSize = _menuWidth - 4;

        foreach (var item in items)
        {
            var button = new FIconButton
            {
                Content = item.Icon,
                Width = buttonSize,
                Height = buttonSize,
                Margin = new Thickness(0),
                CurrentTheme = CurrentTheme,
            };

            button.OnClick += (_, _) =>
            {
                item.OnClick?.Invoke();
                CloseMenu();
            };

            _stackPanel.Children.Add(button);
        }
    }

    private void CreateMixedLayout(MenuItem[] items)
    {
        foreach (var item in items)
        {
            var button = new FTextButton
            {
                Icon = item.Icon,
                ButtonText = item.Text ?? string.Empty,
                Width = _menuWidth - 4,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CurrentTheme = CurrentTheme,
                ContentHorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(0),
            };

            button.OnClick += (_, _) =>
            {
                item.OnClick?.Invoke();
                CloseMenu();
            };

            _stackPanel.Children.Add(button);
        }
    }

    private void CloseMenu()
    {
        Visibility = Visibility.Collapsed;
        _stackPanel.Children.Clear();
        _currentItems.Clear();
        _isFocused = false;
    }

    public bool IsFocused => _isFocused;
    
    public class MenuItem
    {
        public UIElement? Icon { get; set; }
        public string? Text { get; set; }
        public Action? OnClick { get; set; }

        public MenuItem(UIElement? icon, string? text, Action? onClick)
        {
            if (icon == null && string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("MenuItem cannot have both null icon and null/empty text");
            }

            Icon = icon;
            Text = text;
            OnClick = onClick;
        }

        public bool IsIconOnly => Icon != null && string.IsNullOrEmpty(Text);
    }
}
