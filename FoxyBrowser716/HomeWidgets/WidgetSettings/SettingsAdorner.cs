using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        
        public event Action<Dictionary<int, (IWidgetSetting setting, string name)>> CloseRequested;
        
        public SettingsAdorner(Dictionary<int, (IWidgetSetting setting, string name)> settings, string title, Control control) : base(control)
        {
            _control = control;
            _visuals = new VisualCollection(this);

            _grid = new Grid { Background = new SolidColorBrush(Color.FromArgb(100, 50, 50, 50)) };

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
                FontSize = 24,
            });

            var button = new Button
            {
                Content = "Save and Exit",
                Foreground = Brushes.White,
                Background = new SolidColorBrush(AccentColor),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(5),
                Padding = new Thickness(5),
                BorderBrush = new SolidColorBrush(HighlightColor),
                FontSize = 20,
            };
            button.Click += (_, _) => { CloseRequested?.Invoke(settings); };

            titleStack.Children.Add(button);
            
            _settingsHolder.Children.Add(titleStack);
            
            foreach (var kvp in settings.OrderBy(kvp => kvp.Key))
            {
                switch (kvp.Value.setting)
                {
                    case WidgetSettingDescription desc:
                        _settingsHolder.Children.Add(new Label
                        {
                            Content = desc.Value,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Foreground = Brushes.LightGray,
                            FontSize = 16,
                        });
                        break;
                    case WidgetSettingSubTitle desc:
                        _settingsHolder.Children.Add(new Label
                        {
                            Content = desc.Value,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Foreground = Brushes.White,
                            FontSize = 20,
                        });
                        break;
                    case WidgetSettingDiv div:
                        _settingsHolder.Children.Add(new Border()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            BorderBrush = Brushes.DimGray,
                            BorderThickness = new Thickness(1),
                            Margin = new Thickness(3),
                        });
                        break;
                    case WidgetSettingBool boolSetting:
                        var check = new CheckBox
                        {
                            BorderThickness = new Thickness(2),
                            BorderBrush = new SolidColorBrush(HighlightColor),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 20, 0),
                            IsChecked = boolSetting.Value
                        };

                        check.Click += (_, _) =>
                        {
                            kvp.Value.setting.Value = check.IsChecked ?? false;
                            settings.Remove(kvp.Key);
                            settings.TryAdd(kvp.Key, kvp.Value);
                        };

                        _settingsHolder.Children.Add(new Grid()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                            Children =
                            {
                                new Label
                                {
                                    Content = kvp.Value.name,
                                    Foreground = Brushes.White,
                                    HorizontalAlignment = HorizontalAlignment.Left,
                                    FontSize = 16,
                                },
                                check
                            }
                        });
                        break;
                    case WidgetSettingInt intSetting:
                        var intLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var intTextBox = new TextBox
                        {
                            Text = intSetting.Value.ToString(),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        intTextBox.LostFocus += (_, _) =>
                        {
                            if (int.TryParse(intTextBox.Text, out var newValue))
                            {
                                intSetting.Value = newValue;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                            else
                            {
                                intTextBox.Text = intSetting.Value.ToString();
                            }
                        };

                        var intGrid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };
                        
                        var border6 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = intTextBox,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border6.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border6.BorderBrush, Colors.White, HighlightColor);
                        };
                        border6.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border6.BorderBrush, HighlightColor, Colors.White);
                        };

                        intGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        intGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(intLabel, 0);
                        Grid.SetColumn(border6, 1);
                        
                        intGrid.Children.Add(intLabel);
                        intGrid.Children.Add(border6);

                        _settingsHolder.Children.Add(intGrid);
                        break;
                    case WidgetSettingDouble doubleSetting:
                        var doubleLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var doubleTextBox = new TextBox
                        {
                            Text = doubleSetting.Value.ToString(),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        doubleTextBox.LostFocus += (_, _) =>
                        {
                            if (double.TryParse(doubleTextBox.Text, out var newValue))
                            {
                                doubleSetting.Value = newValue;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                            else
                            {
                                doubleTextBox.Text = doubleSetting.Value.ToString();
                            }
                        };

                        var doubleGrid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };
                        
                        var border5 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = doubleTextBox,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border5.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border5.BorderBrush, Colors.White, HighlightColor);
                        };
                        border5.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border5.BorderBrush, HighlightColor, Colors.White);
                        };
                        
                        doubleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        doubleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(doubleLabel, 0);
                        Grid.SetColumn(border5, 1);

                        doubleGrid.Children.Add(doubleLabel);
                        doubleGrid.Children.Add(border5);

                        _settingsHolder.Children.Add(doubleGrid);
                        break;
                    case WidgetSettingString stringSetting:
                        var stringLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var stringTextBox = new TextBox
                        {
                            Text = stringSetting.Value,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        stringTextBox.LostFocus += (_, _) =>
                        {
                            stringSetting.Value = stringTextBox.Text;
                            settings.Remove(kvp.Key);
                            settings.TryAdd(kvp.Key, kvp.Value);
                        };

                        var stringGrid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };
                        
                        var border4 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = stringTextBox,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border4.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border4.BorderBrush, Colors.White, HighlightColor);
                        };
                        border4.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border4.BorderBrush, HighlightColor, Colors.White);
                        };

                        stringGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        stringGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(stringLabel, 0);
                        Grid.SetColumn(border4, 1);
                        
                        stringGrid.Children.Add(stringLabel);
                        stringGrid.Children.Add(border4);

                        _settingsHolder.Children.Add(stringGrid);
                        break;
                    case WidgetSettingFolderPicker filePickerSetting:
                        var fileLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var fileTextBox = new TextBox
                        {
                            Text = filePickerSetting.Value,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        var fileButton = new Button
                        {
                            Content = "Browse",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        fileButton.Click += (_, _) =>
                        {
                            var openFileDialog = new Microsoft.Win32.OpenFolderDialog { Multiselect = false };
                            if (openFileDialog.ShowDialog() == true)
                            {
                                fileTextBox.Text = openFileDialog.FolderName;
                                filePickerSetting.Value = openFileDialog.FolderName;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                        };

                        fileTextBox.LostFocus += (_, _) =>
                        {
                            filePickerSetting.Value = fileTextBox.Text;
                            settings.Remove(kvp.Key);
                            settings.TryAdd(kvp.Key, kvp.Value);
                        };

                        var fileGrid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };

                        fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        fileGrid.ColumnDefinitions.Add(new ColumnDefinition
                            { Width = new GridLength(1, GridUnitType.Star) });
                        fileGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var border2 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = fileTextBox,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border2.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border2.BorderBrush, Colors.White, HighlightColor);
                        };
                        border2.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border2.BorderBrush, HighlightColor, Colors.White);
                        };
                        
                        Grid.SetColumn(fileLabel, 0);
                        Grid.SetColumn(border2, 1);
                        Grid.SetColumn(fileButton, 2);

                        fileGrid.Children.Add(fileLabel);
                        fileGrid.Children.Add(border2);
                        fileGrid.Children.Add(fileButton);

                        _settingsHolder.Children.Add(fileGrid);
                        break;
                    case WidgetSettingFilePicker filePickerSetting:
                        var fileLabel2 = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var fileTextBox2 = new TextBox
                        {
                            Text = filePickerSetting.Value,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        var fileButton2 = new Button
                        {
                            Content = "Browse",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5, 0, 5, 0),
                        };

                        fileButton2.Click += (_, _) =>
                        {
                            var openFileDialog = new Microsoft.Win32.OpenFileDialog { Multiselect = false };
                            if (openFileDialog.ShowDialog() == true)
                            {
                                fileTextBox2.Text = openFileDialog.FileName;
                                filePickerSetting.Value = openFileDialog.FileName;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                        };

                        fileTextBox2.LostFocus += (_, _) =>
                        {
                            filePickerSetting.Value = fileTextBox2.Text;
                            settings.Remove(kvp.Key);
                            settings.TryAdd(kvp.Key, kvp.Value);
                        };

                        var fileGrid2 = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };

                        fileGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        fileGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                        fileGrid2.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var border1 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = fileTextBox2,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border1.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border1.BorderBrush, Colors.White, HighlightColor);
                        };
                        border1.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border1.BorderBrush, HighlightColor, Colors.White);
                        };
                        
                        Grid.SetColumn(fileLabel2, 0);
                        Grid.SetColumn(border1, 1);
                        Grid.SetColumn(fileButton2, 2);

                        fileGrid2.Children.Add(fileLabel2);
                        fileGrid2.Children.Add(border1);
                        fileGrid2.Children.Add(fileButton2);

                        _settingsHolder.Children.Add(fileGrid2);
                        break;
                    case WidgetSettingCombo comboSetting:
                        var comboLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var comboBox = new ComboBox
                        {
                            ItemsSource = comboSetting.Options,
                            SelectedItem = comboSetting.Value,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 0, 20, 0),
                            Width = 150,
                        };

                        comboBox.SelectionChanged += (_, _) =>
                        {
                            if (comboBox.SelectedItem != null)
                            {
                                comboSetting.Value = comboBox.SelectedItem.ToString();
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                        };

                        var comboGrid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };

                        comboGrid.Children.Add(comboLabel);
                        comboGrid.Children.Add(comboBox);

                        _settingsHolder.Children.Add(comboGrid);
                        break;
                    case WidgetSettingColor colorSetting:
                        throw new NotImplementedException(); //TODO: update this to look good
                        var colorLabel = new Label
                        {
                            Content = kvp.Value.name,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 16,
                            Width = 400,
                        };

                        var initialHex =
                            $"#{colorSetting.Value.R:X2}{colorSetting.Value.G:X2}{colorSetting.Value.B:X2}";
                        var colorTextBox = new TextBox
                        {
                            Text = initialHex,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        var colorButton = new Button
                        {
                            Content = "Pick Color",
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(5, 0, 0, 0),
                        };

                        var colorPopup = new Popup
                        {
                            PlacementTarget = colorButton,
                            Placement = PlacementMode.Bottom,
                            StaysOpen = false,
                        };

                        var colorGrid = new Grid
                        {
                            Background = Brushes.White,
                            Width = 100,
                            Height = 100,
                        };

                        // Define color options for the picker
                        Color[] colorOptions =
                            [Colors.Red, Colors.Green, Colors.Blue, Colors.Yellow, Colors.Magenta, Colors.Cyan];
                        var columns = 3;
                        var rows = (int)Math.Ceiling(colorOptions.Length / (double)columns);

                        for (var i = 0; i < rows; i++)
                        {
                            colorGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        }

                        for (var j = 0; j < columns; j++)
                        {
                            colorGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        }

                        var index = 0;
                        foreach (var color in colorOptions)
                        {
                            var colorRect = new Rectangle
                            {
                                Width = 20,
                                Height = 20,
                                Fill = new SolidColorBrush(color),
                                Margin = new Thickness(2),
                            };

                            colorRect.MouseLeftButtonDown += (s, e) =>
                            {
                                var selectedHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                                colorTextBox.Text = selectedHex;
                                colorSetting.Value = color;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                                colorPopup.IsOpen = false;
                            };

                            Grid.SetRow(colorRect, index / columns);
                            Grid.SetColumn(colorRect, index % columns);
                            colorGrid.Children.Add(colorRect);
                            index++;
                        }

                        colorPopup.Child = colorGrid;

                        colorButton.Click += (s, e) => { colorPopup.IsOpen = true; };

                        colorTextBox.LostFocus += (s, e) =>
                        {
                            try
                            {
                                var newColor = (Color)ColorConverter.ConvertFromString(colorTextBox.Text);
                                colorSetting.Value = newColor;
                                settings.Remove(kvp.Key);
                                settings.TryAdd(kvp.Key, kvp.Value);
                            }
                            catch
                            {
                                colorTextBox.Text = initialHex;
                            }
                        };

                        var colorGridLayout = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top,
                            Margin = new Thickness(3),
                        };

                        colorGridLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                        colorGridLayout.ColumnDefinitions.Add(new ColumnDefinition
                            { Width = new GridLength(1, GridUnitType.Star) });
                        colorGridLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                        var border3 = new Border
                        {
                            BorderBrush = new SolidColorBrush(Colors.White),
                            BorderThickness = new Thickness(2),
                            Background = new SolidColorBrush(AccentColor),
                            Margin = new Thickness(25, 0, 0, 0),
                            Child = colorTextBox,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                        };
                        border3.GotKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border3.BorderBrush, Colors.White, HighlightColor);
                        };
                        border3.LostKeyboardFocus += (_, _) =>
                        {
                            ChangeColorAnimation(border3.BorderBrush, HighlightColor, Colors.White);
                        };
                        
                        Grid.SetColumn(colorLabel, 0);
                        Grid.SetColumn(border3, 1);
                        Grid.SetColumn(colorButton, 2);

                        colorGridLayout.Children.Add(colorLabel);
                        colorGridLayout.Children.Add(border3);
                        colorGridLayout.Children.Add(colorButton);

                        _settingsHolder.Children.Add(colorGridLayout);
                        break;
                }
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