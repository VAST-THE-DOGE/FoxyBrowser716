using System.Windows;
using System.Windows.Input;

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

            _startTime = DateTime.Now.Millisecond;
            
            TitleLabel.Content = _tab.Title;
            TabIcon.Child = _tab.Icon;

            Loaded += (s, e) =>
            {
                
            };

            // Hook up event handlers
            LocationChanged += async (s,e) => await Window_LocationChanged(s,e);
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += async (s,e) => await OnMouseLeftButtonUp(s,e);
        }

        private async Task Window_LocationChanged(object? sender, EventArgs e)
        {
            if (ServerManager.Context.DoPositionUpdate(Left, Top) && DateTime.Now.Millisecond > _startTime + 200 && await ServerManager.Context.TryTabTransfer(_tab, Left, Top))
            {
                _isDragging = false;
                Close();
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_dragInitiated)
                {
                    _dragInitiated = true;
                    DragMove(); // Initiates system drag with snapping
                    // After DragMove returns, the drag is complete (mouse button released)
                    OnMouseLeftButtonUp(sender, null);
                }
            }
        }

        private async Task OnMouseLeftButtonUp(object sender, MouseButtonEventArgs? e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            var finalRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            await _instanceManager.CreateAndTransferTabToWindow(_tab, finalRect, 
                WindowState == WindowState.Maximized 
                    ? InstanceManager.BrowserWindowState.Maximized 
                    : InstanceManager.BrowserWindowState.Normal);
            Close();
        }
    }
}