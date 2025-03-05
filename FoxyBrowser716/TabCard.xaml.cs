using static FoxyBrowser716.ColorPalette;
using static FoxyBrowser716.Animator;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FoxyBrowser716
{
    public partial class TabCard : UserControl
    {
        public event Action? DuplicateRequested;
        public event Action? RemoveRequested;
        public event Action? CardClicked;

        public int Key { get; private set; }
        private bool _active = false;

        public TabCard(WebsiteTab tab) : this(tab.TabId, tab) {}
        public TabCard(int key, WebsiteTab tab)
        {
            InitializeComponent();
            Key = key;

            TitleLabel.Content = tab.Title;
            TabIcon.Child = tab.Icon;

            DuplicateButton.Click += (_, _) => DuplicateRequested?.Invoke();
            CloseButton.Click += (_, _) => RemoveRequested?.Invoke();
            MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();

            SetupAnimations();
        }

        public TabCard(int key, TabInfo tab)
        {
            InitializeComponent();
            Key = key;

            DuplicateButton.Visibility = Visibility.Hidden;
            TitleLabel.Content = tab.Title;

            TabIcon.Child = tab.Image;

            CloseButton.Click += (_, _) => RemoveRequested?.Invoke();
            MouseLeftButtonUp += (_, _) => CardClicked?.Invoke();

            SetupAnimations();
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
    }
}