using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using Material.Icons;
using Material.Icons.WPF;

using static FoxyBrowser716.ColorPalette;
using static FoxyBrowser716.Animator;

namespace FoxyBrowser716.HomeWidgets
{
    public class SettingsAdorner : Adorner
    {
        private readonly Control _control;
        private readonly VisualCollection _visuals;
        private readonly Grid _grid;
        private readonly StackPanel _settingsHolder;
        
        public event Action CloseRequested;
        
        public SettingsAdorner(Dictionary<string, IWidgetSetting>? settings, string title, Control control) : base(control)
        {
            _control = control;
            _visuals = new VisualCollection(this);

            _grid = new Grid() { Background = new SolidColorBrush(Color.FromArgb(100, 50, 50, 50)) };

            _settingsHolder = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                CanVerticallyScroll = true,
                Background = Brushes.Transparent,
            };
            
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(HighlightColor),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(MainColor),
                CornerRadius = new CornerRadius(10), 
                Margin = new Thickness(120,30,120,60),
                Child = _settingsHolder,
                Width = _settingsHolder.Width,
                Height = _settingsHolder.Height,
            };

            var titleStack = new Grid()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
            };
            titleStack.Children.Add(new Label
            {
                Content = title,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left,
            });

            var button = new Button
            {
                Content = "Save and Exit",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(AccentColor),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush(HighlightColor),
            };
            button.Click += (s, e) => { CloseRequested?.Invoke(); };

            titleStack.Children.Add(button);
            
            _settingsHolder.Children.Add(titleStack);
            
            foreach (var widgetSetting in settings??[])
            {
                // placeholder:
                _settingsHolder.Children.Add(new Label {Content = widgetSetting.Key, Foreground = Brushes.White});
            }
            
            _grid.Children.Add(border);
            _visuals.Add(_grid);
        }

        protected override int VisualChildrenCount => _visuals.Count;
        protected override Visual GetVisualChild(int index) => _visuals[index];

        protected override Size ArrangeOverride(Size finalSize)
        {
            _grid.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }
}