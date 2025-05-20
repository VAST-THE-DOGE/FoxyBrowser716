using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Material.Icons;
using Material.Icons.WPF;

using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;

namespace FoxyBrowser716.HomeWidgets
{
    public class WidgetOverlayAdorner : Adorner
    {
        private readonly IWidget _widget;
        private readonly VisualCollection _visuals;
        private readonly Grid _grid;
        private readonly Border _border;
        private readonly Ellipse _circle;
        private readonly Ellipse _resizeHandle;
        private readonly StackPanel _iconPanel;
        private bool _isDragging;
        private bool _isResizing;

        public WidgetOverlayAdorner(IWidget widget) : base(widget)
        {
            _widget = widget;
            _visuals = new VisualCollection(this);

            _grid = new Grid();

            _border = new Border
            {
                BorderBrush = new SolidColorBrush(HighlightColor),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(100, 50, 50, 50))
            };

            _circle = new Ellipse
            {
                Width = 25,
                Height = 25,
                Stroke = new SolidColorBrush(HighlightColor),
                Fill = new SolidColorBrush(AccentColor),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(5, 5, 0, 0),
                Cursor = Cursors.SizeAll
            };

            _resizeHandle = new Ellipse
            {
                Width = 15,
                Height = 15,
                Stroke = new SolidColorBrush(HighlightColor),
                Fill = new SolidColorBrush(AccentColor),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 5, 5),
                Cursor = Cursors.SizeNWSE
            };

            _iconPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top
            };

            if (_widget.WidgetSettings?.Count > 0)
            {
                var settingsButton = new Button
                {
                    Content = new MaterialIcon
                    {
                        Kind = MaterialIconKind.Gear,
                        Foreground = Brushes.DodgerBlue,
                        Width = 15,
                        Height = 15
                    },
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Command = _widget.SettingsCommand,
                    Margin = new Thickness(0, 5, 5, 0)
                };
                settingsButton.MouseEnter += (_, _) => { ChangeColorAnimation(settingsButton.Background, Transparent, AccentColor); };
                settingsButton.MouseLeave += (_, _) => { ChangeColorAnimation(settingsButton.Background, AccentColor, Transparent); };
                _iconPanel.Children.Add(settingsButton);
            }

            if (_widget.CanRemove)
            {
                var removeButton = new Button
                {
                    Content = new MaterialIcon
                    {
                        Kind = MaterialIconKind.Remove,
                        Foreground = Brushes.Red,
                        Width = 15,
                        Height = 15
                    },
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(0),
                    Command = _widget.RemoveCommand,
                    Margin = new Thickness(0, 5, 5, 0)
                };
                removeButton.MouseEnter += (_, _) => { ChangeColorAnimation(removeButton.Background, Transparent, AccentColor); };
                removeButton.MouseLeave += (_, _) => { ChangeColorAnimation(removeButton.Background, AccentColor, Transparent); };
                _iconPanel.Children.Add(removeButton);
            }

            _grid.Children.Add(_border);
            _grid.Children.Add(_circle);
            _grid.Children.Add(_resizeHandle);
            _grid.Children.Add(_iconPanel);
            _visuals.Add(_grid);

            _circle.MouseLeftButtonDown += Circle_MouseLeftButtonDown;
            _circle.MouseMove += Circle_MouseMove;
            _circle.MouseLeftButtonUp += Circle_MouseLeftButtonUp;

            _resizeHandle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
            _resizeHandle.MouseMove += ResizeHandle_MouseMove;
            _resizeHandle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;
        }

        private void Circle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _circle.CaptureMouse();
            e.Handled = true;
        }

        private void Circle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                if (_widget.Parent is Grid grid)
                {
                    var position = e.GetPosition(grid);
                    var newRow = GetRowFromPosition(position, grid);
                    var newColumn = GetColumnFromPosition(position, grid);
                    Grid.SetRow(_widget, newRow);
                    Grid.SetColumn(_widget, newColumn);
                }
            }
        }

        private void Circle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                _circle.ReleaseMouseCapture();
                _widget.Data.Row = Grid.GetRow(_widget);
                _widget.Data.Column = Grid.GetColumn(_widget);
            }
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _resizeHandle.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                if (_widget.Parent is Grid grid)
                {
                    var position = e.GetPosition(grid);
                    var targetRow = GetRowFromPosition(position, grid);
                    var targetColumn = GetColumnFromPosition(position, grid);
                    var currentRow = Grid.GetRow(_widget);
                    var currentColumn = Grid.GetColumn(_widget);
                    var potentialRowSpan = targetRow - currentRow + 1;
                    var potentialColumnSpan = targetColumn - currentColumn + 1;
                    var newRowSpan = Math.Max(_widget.MinWidgetHeight, Math.Min(_widget.MaxWidgetHeight, potentialRowSpan));
                    var newColumnSpan = Math.Max(_widget.MinWidgetWidth, Math.Min(_widget.MaxWidgetWidth, potentialColumnSpan));
                    Grid.SetRowSpan(_widget, newRowSpan);
                    Grid.SetColumnSpan(_widget, newColumnSpan);
                }
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _resizeHandle.ReleaseMouseCapture();
                _widget.Data.RowSpan = Grid.GetRowSpan(_widget);
                _widget.Data.ColumnSpan = Grid.GetColumnSpan(_widget);
            }
        }

        // Helper methods to calculate grid position
        private int GetRowFromPosition(Point position, Grid grid)
        {
            double y = 0;
            for (var row = 0; row < grid.RowDefinitions.Count; row++)
            {
                y += grid.RowDefinitions[row].ActualHeight;
                if (position.Y < y)
                    return row;
            }
            return grid.RowDefinitions.Count - 1;
        }

        private int GetColumnFromPosition(Point position, Grid grid)
        {
            double x = 0;
            for (var col = 0; col < grid.ColumnDefinitions.Count; col++)
            {
                x += grid.ColumnDefinitions[col].ActualWidth;
                if (position.X < x)
                    return col;
            }
            return grid.ColumnDefinitions.Count - 1;
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