using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static FoxyBrowser716.Styling.ColorPalette;

namespace FoxyBrowser716
{
    public partial class TabMoveWindowCard : Window
    {
        private WebsiteTab _tab;
        private bool _isDragging = true;
        private bool _dragInitiated = false;
        private double _startTime;
        private InstanceManager  _instanceManager;

        public TabMoveWindowCard(WebsiteTab tab, InstanceManager instanceManager)
        {
            InitializeComponent();
            _tab = tab;
            _instanceManager = instanceManager;

            RootBorder.BorderBrush = new SolidColorBrush(HighlightColor);
            
            _startTime = DateTime.Now.Millisecond;
            
            TitleLabel.Content = _tab.Title;
            TabIcon.Child = _tab.Icon;

            Loaded += (_, _) =>
            {
                LocationChanged += async (s,e) => await Window_LocationChanged(s,e);
            };

            MouseMove += OnMouseMove;
            MouseLeftButtonUp += async (s,e) => await OnMouseLeftButtonUp(s,e);
        }

        private async Task Window_LocationChanged(object? sender, EventArgs e)
        {
            // if (!IsVisible) return;
            var mid = new Point(Left + RootBorder.Width / 2, Top + RootBorder.Height / 2);
            // Point mid = Mouse.GetPosition(null);
            //var mid = PointToScreen(Mouse.GetPosition(this));
            
            // var mid = Mouse.GetPosition(RootBorder);
            // var mid = new Point(Left + mouse.X, Top + mouse.Y);
            if (DateTime.Now.Millisecond > _startTime + 300 && ServerManager.Context.DoPositionUpdate(mid) && await ServerManager.Context.TryTabTransfer(_tab, mid))
            {
                _isDragging = false;
                Close();
            }
        }

        private async void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_dragInitiated)
                {
                    _dragInitiated = true;
                    DragMove();
                    await OnMouseLeftButtonUp(sender, null);
                }
            }
        }

        private async Task OnMouseLeftButtonUp(object sender, MouseButtonEventArgs? e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            var finalRect = new Rect(Left, Top, Width == 280 ? 0 : Width, Height == 45 ? 0 : Height);
            await _instanceManager.CreateAndTransferTabToWindow(_tab, finalRect, 
                WindowState == WindowState.Maximized 
                    ? InstanceManager.BrowserWindowState.Maximized 
                    : InstanceManager.BrowserWindowState.Normal);
            Close();
        }
    }
}