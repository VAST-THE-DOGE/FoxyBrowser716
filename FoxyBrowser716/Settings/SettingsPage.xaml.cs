using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using FoxyBrowser716.Styling;

namespace FoxyBrowser716.Settings;

public class PrimarySettingsPage
{
    public static SettingsPage GeneratePage(BrowserSettingsManager manager)
    {
        var settingsPage = new SettingsPage();

        #region General
        var general= new SettingsCategory("General");
        
        #endregion
        #region Appearance
        var appearance = new SettingsCategory("Appearance");

        #endregion
        #region WebView
        var webview = new SettingsCategory("WebView2 Specific");

        #endregion
        #region Extensions
        var extensions = new SettingsCategory("Extensions");

        #endregion
        #region Instances
        var instances = new SettingsCategory("Instances");
        
        #endregion
        #region Misc
        var misc = new SettingsCategory("Misc");

        #endregion
        // EXAMPLE:
        // General Settings Category
        // generalCategory.AddSetting(new SubheadingSetting("Application Behavior"));
        // generalCategory.AddSetting(new BoolSetting("Start on system boot", "Open application when your computer starts", false));
        // generalCategory.AddSetting(new BoolSetting("Auto-update", "Automatically download and install updates", true));
        // generalCategory.AddSetting(new DividerSetting());
        // generalCategory.AddSetting(new SubheadingSetting("Default Browser"));
        // generalCategory.AddSetting(new BoolSetting("Make default browser", "Set as the default browser for web links", false));
        // generalCategory.AddSetting(new DividerSetting());
        // generalCategory.AddSetting(new CustomControlSetting("Custom Setting Example", () => 
        // {
        //     var panel = new StackPanel { Orientation = Orientation.Vertical };
        //     var button = new Button { 
        //         Content = "Click Me", 
        //         Padding = new Thickness(10, 5, 10, 5),
        //         Background = new SolidColorBrush(ColorPalette.AccentColor),
        //         Foreground = new SolidColorBrush(Colors.White),
        //         BorderBrush = new SolidColorBrush(ColorPalette.HighlightColor),
        //         BorderThickness = new Thickness(1),
        //         HorizontalAlignment = HorizontalAlignment.Left
        //     };
        //     button.Click += (_, _) => MessageBox.Show("Custom control clicked!");
        //     panel.Children.Add(button);
        //     return panel;
        // }));
        //     
        // // Privacy Settings Category
        // var privacyCategory = new SettingsCategory("Privacy");
        // privacyCategory.AddSetting(new DescriptionSetting("Configure how your data is handled and protected."));
        // privacyCategory.AddSetting(new BoolSetting("Block third-party cookies", "Prevent websites from storing tracking cookies", true));
        // privacyCategory.AddSetting(new BoolSetting("Do Not Track", "Request websites not to track your browsing", true));
        // privacyCategory.AddSetting(new BoolSetting("Clear browsing data on exit", "Automatically clear your browsing history when closing", false));
        //     
        // // Appearance Settings Category
        // var appearanceCategory = new SettingsCategory("Appearance");
        // appearanceCategory.AddSetting(new SubheadingSetting("Theme"));
        // appearanceCategory.AddSetting(new BoolSetting("Dark mode", "Use dark colors for the interface", true));
        // appearanceCategory.AddSetting(new BoolSetting("Use system theme", "Match your operating system's theme", false));
        // appearanceCategory.AddSetting(new DividerSetting());
        // appearanceCategory.AddSetting(new SubheadingSetting("Tabs"));
        // appearanceCategory.AddSetting(new BoolSetting("Show tab previews", "Display thumbnail previews when hovering over tabs", true));
        // appearanceCategory.AddSetting(new StringSetting("Home page URL", "The page that opens when you click the home button", "https://www.google.com"));
        //     
        // // Advanced Settings Category
        // var advancedCategory = new SettingsCategory("Advanced");
        // advancedCategory.AddSetting(new SubheadingSetting("Performance"));
        // advancedCategory.AddSetting(new BoolSetting("Hardware acceleration", "Use your graphics card to speed up rendering", true));
        // advancedCategory.AddSetting(new BoolSetting("Background processes", "Allow processes to run when browser is closed", false));
        // advancedCategory.AddSetting(new DividerSetting());
        // advancedCategory.AddSetting(new SubheadingSetting("Developer Tools"));
        // advancedCategory.AddSetting(new BoolSetting("Enable developer tools", "Show developer tools for debugging websites", false));
        //     
        // // Extensions Category
        // var extensionsCategory = new SettingsCategory("Extensions");
        // extensionsCategory.AddSetting(new DescriptionSetting("Manage your browser extensions and add-ons."));
        // extensionsCategory.AddSetting(new CustomControlSetting("Installed Extensions", () => 
        // {
        //     var panel = new StackPanel { Orientation = Orientation.Vertical };
        //     // This would typically be populated with installed extensions
        //     panel.Children.Add(new TextBlock { 
        //         Text = "No extensions installed",
        //         Foreground = new SolidColorBrush(Colors.LightGray)
        //     });
        //     return panel;
        // }));
            
        // Add all categories to the settings page
        settingsPage.AddCategories(new[] { 
            general, 
            appearance,
            webview,
            extensions,
            instances,
            misc,
        });
            
        return settingsPage;
    }
}
    
public abstract class Setting<T> : ISetting
{
    public string Name { get; }
    public string Description { get; }
    public T Value { get; set; }
    public Action<T> ValueChanged { get; set; }
        
    protected Setting(string name, string description, T defaultValue)
    {
        Name = name;
        Description = description;
        Value = defaultValue;
    }
        
    public abstract UIElement GetSettingControl();
        
    protected void OnValueChanged(T newValue)
    {
        Value = newValue;
        ValueChanged?.Invoke(newValue);
    }
}
    
/// <summary>
/// A setting that acts as a section divider
/// </summary>
public class DividerSetting : ISetting
{
    public string Name => string.Empty;
        
    public UIElement GetSettingControl()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Colors.Gray),
            Margin = new Thickness(0, 10, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }
}
    
/// <summary>
/// A setting that displays a subheading
/// </summary>
public class SubheadingSetting : ISetting
{
    public string Name { get; }
        
    public SubheadingSetting(string name)
    {
        Name = name;
    }
        
    public UIElement GetSettingControl()
    {
        return new TextBlock
        {
            Text = Name,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 15, 0, 5)
        };
    }
}
    
/// <summary>
/// A setting that displays descriptive text
/// </summary>
public class DescriptionSetting : ISetting
{
    public string Name { get; }
    public string Description { get; }
        
    public DescriptionSetting(string description)
    {
        Name = string.Empty;
        Description = description;
    }
        
    public UIElement GetSettingControl()
    {
        return new TextBlock
        {
            Text = Description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Colors.LightGray),
            Margin = new Thickness(0, 0, 0, 10)
        };
    }
}
    
/// <summary>
/// A boolean setting displayed as a checkbox
/// </summary>
public class BoolSetting : Setting<bool>
{
    public BoolSetting(string name, string description, bool defaultValue = false) 
        : base(name, description, defaultValue)
    {
    }
        
    public override UIElement GetSettingControl()
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 5, 0, 5)
        };
            
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            
        var nameLabel = new TextBlock
        {
            Text = Name,
            Foreground = new SolidColorBrush(Colors.White),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(nameLabel, 0);
            
        var checkbox = new CheckBox
        {
            IsChecked = Value,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(5, 0, 0, 0),
            BorderThickness = new Thickness(2),
            BorderBrush = new SolidColorBrush(ColorPalette.HighlightColor)
        };
            
        checkbox.Checked += (_, _) => OnValueChanged(true);
        checkbox.Unchecked += (_, _) => OnValueChanged(false);
        Grid.SetColumn(checkbox, 1);
            
        if (!string.IsNullOrEmpty(Description))
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
            var descriptionBlock = new TextBlock
            {
                Text = Description,
                Foreground = new SolidColorBrush(Colors.LightGray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 12
            };
            Grid.SetRow(descriptionBlock, 1);
            Grid.SetColumnSpan(descriptionBlock, 2);
                
            Grid.SetRow(nameLabel, 0);
            Grid.SetRow(checkbox, 0);
                
            grid.Children.Add(descriptionBlock);
        }
            
        grid.Children.Add(nameLabel);
        grid.Children.Add(checkbox);
            
        return grid;
    }
}
    
/// <summary>
/// A string setting displayed as a text box
/// </summary>
public class StringSetting : Setting<string>
{
    public StringSetting(string name, string description, string defaultValue = "") 
        : base(name, description, defaultValue)
    {
    }
        
    public override UIElement GetSettingControl()
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 8, 0, 8)
        };
            
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
        if (!string.IsNullOrEmpty(Description))
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
            
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
        var nameLabel = new TextBlock
        {
            Text = Name,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(nameLabel, 0);

        var border = new Border()
        {
            Padding = new Thickness(0, 0, 0, 0),
            Background = new SolidColorBrush(ColorPalette.AccentColor),
            BorderBrush = new SolidColorBrush(Colors.White),
            BorderThickness = new Thickness(2),
            Margin = new Thickness(0, 0, 0, 0)
        };
        
        var textBox = new TextBox
        {
            Text = Value,
            Padding = new Thickness(8, 5, 8, 5),
            Background = new SolidColorBrush(Colors.Transparent),
            Foreground = new SolidColorBrush(Colors.White),
            BorderBrush = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 0)
        };
            
        textBox.GotKeyboardFocus += (_, _) =>
        {
            var brush = border.BorderBrush as SolidColorBrush;
            var animation = new ColorAnimation
            {
                To = ColorPalette.HighlightColor,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            brush?.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        };
            
        textBox.LostKeyboardFocus += (_, _) =>
        {
            var brush = border.BorderBrush as SolidColorBrush;
            var animation = new ColorAnimation
            {
                To = Colors.White,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            brush?.BeginAnimation(SolidColorBrush.ColorProperty, animation);
                
            OnValueChanged(textBox.Text);
        };
            
        int descriptionOffset = 0;
            
        if (!string.IsNullOrEmpty(Description))
        {
            var descriptionBlock = new TextBlock
            {
                Text = Description,
                Foreground = new SolidColorBrush(Colors.LightGray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12
            };
            Grid.SetRow(descriptionBlock, 1);
            grid.Children.Add(descriptionBlock);
            descriptionOffset = 1;
        }
            
        border.Child = textBox;
        
        Grid.SetRow(border, 1 + descriptionOffset);
            
        grid.Children.Add(nameLabel);
        grid.Children.Add(border);
            
        return grid;
    }
}
    
/// <summary>
/// A placeholder for custom controls
/// </summary>
public class CustomControlSetting : ISetting
{
    public string Name { get; }
    private readonly Func<UIElement> _controlFactory;
        
    public CustomControlSetting(string name, Func<UIElement> controlFactory)
    {
        Name = name;
        _controlFactory = controlFactory;
    }
        
    public UIElement GetSettingControl()
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 8, 0, 8)
        };
            
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
        var nameLabel = new TextBlock
        {
            Text = Name,
            Foreground = new SolidColorBrush(Colors.White),
            Margin = new Thickness(0, 0, 0, 5)
        };
        Grid.SetRow(nameLabel, 0);
            
        var customControl = _controlFactory();
        Grid.SetRow(customControl, 1);
            
        grid.Children.Add(nameLabel);
        grid.Children.Add(customControl);
            
        return grid;
    }
}
        
public class SettingsCategory
{
    public string Name { get; set; }
    public ObservableCollection<ISetting> Settings { get; } = new ObservableCollection<ISetting>();
        
    public SettingsCategory(string name)
    {
        Name = name;
    }
        
    public void AddSetting(ISetting setting)
    {
        Settings.Add(setting);
    }
        
    public void AddSettings(IEnumerable<ISetting> settings)
    {
        foreach (var setting in settings)
        {
            Settings.Add(setting);
        }
    }
}
    
/// <summary>
/// Interface for all settings
/// </summary>
public interface ISetting
{
    string Name { get; }
    UIElement GetSettingControl();
}
    
/// <summary>
/// A comprehensive settings page that displays categories on the left and settings on the right
/// </summary>
public partial class SettingsPage : UserControl
{
    // Collection of settings categories
    private ObservableCollection<SettingsCategory> _categories = new ObservableCollection<SettingsCategory>();
        
    // Currently active category
    private string _activeCategory;
        
    // UI elements for categories
    private Dictionary<string, Border> _categoryElements = new Dictionary<string, Border>();
        
    // Dictionary to keep track of category positions in the scroll
    private Dictionary<string, double> _categoryPositions = new Dictionary<string, double>();
        
    private ScrollViewer _settingsScrollViewer;
        
    public SettingsPage()
    {
        InitializeComponent();
            
        // Listen for scroll changes to update active category
        Loaded += (_, _) => {
            _settingsScrollViewer = SettingsScrollViewer;
            if (_settingsScrollViewer != null)
            {
                _settingsScrollViewer.ScrollChanged += OnSettingsScrollChanged;
            }
        };
    }
        
    /// <summary>
    /// Adds a category of settings to the settings page
    /// </summary>
    public void AddCategory(SettingsCategory category)
    {
        _categories.Add(category);
        RebuildSettingsUI();
    }
        
    /// <summary>
    /// Adds multiple categories to the settings page
    /// </summary>
    public void AddCategories(IEnumerable<SettingsCategory> categories)
    {
        foreach (var category in categories)
        {
            _categories.Add(category);
        }
        RebuildSettingsUI();
    }
        
    /// <summary>
    /// Rebuilds the entire settings UI
    /// </summary>
    private void RebuildSettingsUI()
    {
        // Clear existing UI
        CategoriesPanel.Children.Clear();
        SettingsPanel.Children.Clear();
        _categoryElements.Clear();
        _categoryPositions.Clear();
            
        // Skip if no categories
        if (!_categories.Any())
            return;
                
        // Set first category as active if none selected
        if (string.IsNullOrEmpty(_activeCategory))
            _activeCategory = _categories.First().Name;
            
        // Build categories list
        foreach (var category in _categories)
        {
            var categoryBorder = new Border
            {
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(0, 2, 0, 2),
                CornerRadius = new CornerRadius(4),
            };
                
            if (category.Name == _activeCategory)
            {
                categoryBorder.Background = new SolidColorBrush(ColorPalette.HighlightColor);
            }
            else
            {
                categoryBorder.Background = new SolidColorBrush(Colors.Transparent);
                categoryBorder.MouseEnter += (_, _) =>
                {
                    Animator.ChangeColorAnimation(
                        categoryBorder.Background, 
                        ColorPalette.Transparent, 
                        Color.FromArgb(150, (byte)(ColorPalette.AccentColor.R + 30), (byte)(ColorPalette.AccentColor.G + 30), (byte)(ColorPalette.AccentColor.B + 30)), 
                        0.25);
                };
                    
                categoryBorder.MouseLeave += (_, _) =>
                {
                    if (category.Name != _activeCategory)
                    {
                        Animator.ChangeColorAnimation(
                            categoryBorder.Background, 
                            Color.FromArgb(150, (byte)(ColorPalette.AccentColor.R + 30), (byte)(ColorPalette.AccentColor.G + 30), (byte)(ColorPalette.AccentColor.B + 30)), 
                            ColorPalette.Transparent, 
                            0.5);
                    }
                };
            }
                
            categoryBorder.MouseLeftButtonDown += (_, _) => JumpToCategory(category.Name);
                
            var categoryText = new TextBlock
            {
                Text = category.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 16
            };
                
            categoryBorder.Child = categoryText;
            _categoryElements[category.Name] = categoryBorder;
            CategoriesPanel.Children.Add(categoryBorder);
        }
            
        // Add settings content
        double currentPosition = 0;
            
        foreach (var category in _categories)
        {
            // Add a marker for this category
            var marker = new FrameworkElement { Tag = category.Name };
            marker.Loaded += CategoryMarker_Loaded;
            SettingsPanel.Children.Add(marker);
        
            // Add category header
            var categoryHeader = new Border
            {
                Padding = new Thickness(0, 10, 0, 5),
                Margin = new Thickness(0, 0, 0, 15),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255))
            };

                
            var headerText = new TextBlock
            {
                Text = category.Name,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold
            };
                
            categoryHeader.Child = headerText;
            SettingsPanel.Children.Add(categoryHeader);
                
            // Record the starting position of this category
            _categoryPositions[category.Name] = currentPosition;
            currentPosition += categoryHeader.Padding.Top + categoryHeader.Padding.Bottom + 
                               categoryHeader.Margin.Top + categoryHeader.Margin.Bottom + 30; // Estimated height
                
            // Add settings
            foreach (var setting in category.Settings)
            {
                var control = setting.GetSettingControl();
                SettingsPanel.Children.Add(control);
                    
                // Estimate the height contribution (this is approximate)
                currentPosition += (control as FrameworkElement)?.Height ?? 30;
                currentPosition += (control as FrameworkElement)?.Margin.Top ?? 0;
                currentPosition += (control as FrameworkElement)?.Margin.Bottom ?? 0;
            }
                
            // Add bottom divider after each category except the last one
            if (category != _categories.Last())
            {
                var divider = new Border
                {
                    Height = 1,
                    Margin = new Thickness(0, 15, 0, 15),
                    Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255))
                };
                    
                SettingsPanel.Children.Add(divider);
                currentPosition += 31; // divider height + margins
            }
        }
    }
        
    private void CategoryMarker_Loaded(object sender, RoutedEventArgs e)
    {
        var marker = sender as FrameworkElement;
        if (marker != null && _settingsScrollViewer != null)
        {
            var categoryName = marker.Tag as string;
            if (!string.IsNullOrEmpty(categoryName))
            {
                // Calculate the actual position based on the marker's position in the scroll viewer
                var markerTransform = marker.TransformToAncestor(_settingsScrollViewer);
                var markerPosition = markerTransform.Transform(new Point(0, 0)).Y;
                _categoryPositions[categoryName] = markerPosition;
            }
        }
    }

        
    /// <summary>
    /// Called when the settings panel is scrolled to update the active category
    /// </summary>
    private void OnSettingsScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_categoryPositions.Count == 0)
            return;
                
        // Find the category that should be active based on scroll position
        var verticalOffset = e.VerticalOffset;
        string newActiveCategory = _categoryPositions
            .OrderBy(kv => kv.Value)
            .Last(kv => kv.Value <= verticalOffset + 10).Key;
            
        if (newActiveCategory != _activeCategory)
        {
            // Update category highlighting
            if (_categoryElements.ContainsKey(_activeCategory))
            {
                var oldCategoryBorder = _categoryElements[_activeCategory];
                Animator.ChangeColorAnimation(
                    oldCategoryBorder.Background, 
                    ColorPalette.HighlightColor, 
                    ColorPalette.Transparent, 
                    0.20);
            }
                
            if (_categoryElements.ContainsKey(newActiveCategory))
            {
                var newCategoryBorder = _categoryElements[newActiveCategory];
                Animator.ChangeColorAnimation(
                    newCategoryBorder.Background, 
                    ColorPalette.Transparent, 
                    ColorPalette.HighlightColor, 
                    0.20);
            }
                
            _activeCategory = newActiveCategory;
        }
    }
        
    /// <summary>
    /// Jumps to a specific category in the settings
    /// </summary>
    private void JumpToCategory(string categoryName)
    {
        if (_categoryPositions.ContainsKey(categoryName) && _settingsScrollViewer != null)
        {
            _settingsScrollViewer.ScrollToVerticalOffset( _categoryPositions[categoryName]);

            _activeCategory = categoryName;
                
            foreach (var element in _categoryElements)
            {
                var border = element.Value;
                var brush = border.Background as SolidColorBrush;
                    
                if (element.Key == categoryName)
                {
                    Animator.ChangeColorAnimation(
                        border.Background, 
                        ColorPalette.Transparent, 
                        ColorPalette.HighlightColor, 
                        0.20);
                }
                else
                {
                    Animator.ChangeColorAnimation(
                        border.Background, 
                        brush?.Color??ColorPalette.Transparent, 
                        ColorPalette.Transparent, 
                        0.20);
                }
            }
        }
    }
}