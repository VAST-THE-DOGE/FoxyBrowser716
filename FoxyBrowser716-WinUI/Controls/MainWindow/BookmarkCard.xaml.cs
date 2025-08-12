using FoxyBrowser716_WinUI.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;

namespace FoxyBrowser716_WinUI.Controls.MainWindow;

public sealed partial class BookmarkCard : UserControl
{
    public event Action<string>? NoteChanged;
    public event Action? RemoveRequested;
    public event Action? OnClick;
    
    public BookmarkCard()
    {
        InitializeComponent();
        ApplyTheme();
    }

    public BookmarkCard(WebsiteInfo websiteInfo) : this()
    {
        websiteInfo.PropertyChanged += (_, _) => RefreshData(websiteInfo);
        RefreshData(websiteInfo);
    }

    private void RefreshData(WebsiteInfo websiteInfo)
    {
        Icon.Child = new Image
        {
            Source = new BitmapImage(string.IsNullOrWhiteSpace(websiteInfo.FavIconUrl) ? new Uri("https://TODO") : new Uri(websiteInfo.FavIconUrl)), //TODO
            Width = 18, Height = 18,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        Label.Text = websiteInfo.Title;
        NoteInput.SetText(websiteInfo.Note);
    }

    internal Theme CurrentTheme
    {
        get;
        set
        {
            field = value;
            ApplyTheme();
        }
    } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        Root.BorderBrush = new SolidColorBrush(CurrentTheme.SecondaryBackgroundColor);
        Root.Background = new SolidColorBrush(MouseOver ? CurrentTheme.PrimaryAccentColorSlightTransparent : CurrentTheme.PrimaryBackgroundColorVeryTransparent);

        if (Icon.Child is FrameworkElement iconElement)
        {
            iconElement.SetValue(ForegroundProperty, new SolidColorBrush(CurrentTheme.PrimaryForegroundColor));
        }
        Label.Foreground = new SolidColorBrush(CurrentTheme.PrimaryForegroundColor);

        ButtonClose.CurrentTheme = CurrentTheme with{ PrimaryAccentColor = CurrentTheme.NoColor};
        NoteInput.CurrentTheme = CurrentTheme;
    }

    
    private bool MouseOver;
    private void Root_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = true;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryAccentColorSlightTransparent);
    }

    private void Root_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        MouseOver = false;
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent);
    }

    private void ButtonClose_OnOnClick(object sender, RoutedEventArgs e)
    {
        RemoveRequested?.Invoke();
    }

    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonClose.PointerOver) return;
        
        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryBackgroundColorVeryTransparent, 0.3);
        OnClick?.Invoke();
    }
    
    private void Root_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (ButtonClose.PointerOver) return;

        ChangeColorAnimation(Root.Background, CurrentTheme.PrimaryHighlightColorSlightTransparent, 0.05);
    }

    private void NoteInput_OnOnTextChanged(string note)
    {
        NoteChanged?.Invoke(note);
    }

    private void NoteInput_OnEnterPressed()
    {
        Label.Focus(FocusState.Programmatic); // to get focus off of textbox
    }
}