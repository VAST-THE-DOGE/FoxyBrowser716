using FoxyBrowser716_WinUI.DataObjects.Basic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace FoxyBrowser716_WinUI.Controls.Generic;

public sealed partial class FContextMenu : UserControl
{
    private StackPanel _stackPanel;
    private Border _border;
    private Popup _popup;
    private double _menuWidth;
    public event Action? OnClose;

    internal Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    public FContextMenu()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        VerticalAlignment = VerticalAlignment.Top;
        HorizontalAlignment = HorizontalAlignment.Left;
        
        _stackPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        
        _border = new Border
        {
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(10),
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = _stackPanel,
        };

        _popup = new Popup
        {
            Child = _border,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
        };

        _popup.Closed += (_, _) => OnClose?.Invoke();
        
        Content = _popup;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        _border.Background = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorVeryTransparent);
        _border.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColorSlightTransparent);

        if (_stackPanel.Children is not null)
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

    public void SetItems(IEnumerable<MenuItem> items, double iconWidth = 1000) // quick refactor, but this is changed to only affect icons
    {
        var itemsA = items.ToArray();
        
        _menuWidth = iconWidth;
            
        _stackPanel.Children.Clear();

        if (itemsA.Length == 0)
        {
            _popup.IsOpen = false;
            return;
        }
        
        var allIconOnly = itemsA.All(item => item.IsIconOnly);

        if (allIconOnly)
        {
            CreateIconOnlyLayout(itemsA);
        }
        else
        {
            CreateMixedLayout(itemsA);
        }
        
        _popup.IsOpen = true;
    }

    private void CreateIconOnlyLayout(MenuItem[] items)
    {
        var buttonSize = _menuWidth - 4;

        foreach (var item in items)
        {
            var button = new FIconButton
            {
                Content = item.Icon,
                Padding = new Thickness(item.IconPadding),
                Width = buttonSize,
                Height = buttonSize,
                Margin = new Thickness(0),
                CurrentTheme = CurrentTheme,
            };

            button.OnClick += (_, _) =>
            {
                item.OnClick?.Invoke();
                if (item.CloseOnClick)
                    _popup.IsOpen = false;
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
                Padding = new Thickness(item.IconPadding),
                ButtonText = item.Text ?? string.Empty,
                //Width = _menuWidth - 4,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                CurrentTheme = CurrentTheme,
                ContentHorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(0),
            };

            button.OnClick += (_, _) =>
            {
                item.OnClick?.Invoke();
                if (item.CloseOnClick)
                    _popup.IsOpen = false;
            };

            _stackPanel.Children.Add(button);
        }
    }
    
    public class MenuItem
    {
        public UIElement? Icon { get; set; }
        public string? IconUri { get; set; }

        public double IconPadding { get; set; }

        public string? Text { get; set; }
        public Action? OnClick { get; set; }
        
        public bool CloseOnClick { get; set; }

        public MenuItem(UIElement? icon, double iconPadding, string? text, Action? onClick, bool closeOnClick = true)
        {
            if (icon == null && string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("MenuItem cannot have both null icon and null/empty text");
            }

            Icon = icon;
            IconPadding = iconPadding;
            Text = text;
            OnClick = onClick;
            CloseOnClick = closeOnClick;
        }
        
        public MenuItem(string text, Action? onClick)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("MenuItem cannot have both null icon and null/empty text");
            }
            
            Text = text;
            OnClick = onClick;
        }

        public bool IsIconOnly => Icon != null && string.IsNullOrEmpty(Text);
    }
}
