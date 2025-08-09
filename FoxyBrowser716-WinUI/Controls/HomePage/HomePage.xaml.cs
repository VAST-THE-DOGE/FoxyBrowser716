using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using FoxyBrowser716_WinUI.Controls.HomePage.Widgets;
using FoxyBrowser716_WinUI.Controls.MainWindow;
using FoxyBrowser716_WinUI.DataManagement;
using FoxyBrowser716_WinUI.DataObjects.Settings;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716_WinUI.Controls.HomePage;

public sealed partial class HomePage : UserControl
{
    private const string WidgetsFileName = "WidgetLayout.json";
    private const string SettingsFileName = "HomePageSettings.json";
    // private const string DefaultBackgroundName = "FoxyBrowserDefaultBackground.jpg";
    private List<WidgetData> _savedWidgets;
    private HomeSettings _settings;
    private TabManager _manager;
    private Instance _instanceData;
    
    private WidgetEditOverlay? _hoveredOverlay;
    
    private const int ColumnCount = 40;
    private const int RowCount = 40;
    
    const int MidwayColumn = ColumnCount / 2;
    const int MidwayRow = RowCount / 2;

    
    public event Action<bool>? ToggleEditMode;
    public bool InEditMode { get; private set; }

    internal static void AddWidget(string name, MaterialIconKind icon, WidgetCategory category, Func<TabManager, Dictionary<string,object>?, WidgetData, Task<WidgetBase>> factory)
    {
        if (!AvaliableWidgets.TryAdd(name, (icon, category, factory)))
            throw new Exception($"Widget with name '{name}' already registered");
    }

    private static readonly Dictionary<string, (MaterialIconKind Icon, WidgetCategory Category, Func<TabManager, Dictionary<string, object>?, WidgetData, Task<WidgetBase>> Factory)>
        AvaliableWidgets = []; // Updated by assembly load. All that is needed is to make the class that inherits from Widget<T>

    private System.Timers.Timer _updateTimer;
    
    public HomePage()
    {
        InitializeComponent();
    }

    public async Task Initialize(TabManager manager, Instance instanceManager)
    {
        _manager = manager; // save manager for later use
        _instanceData = instanceManager;
        
        // setup columns/rows
        const double percentagePerColumn = 100.0 / ColumnCount;
        for (var i = 0; i < ColumnCount; i++)
        {
            var columnDef = new ColumnDefinition
            {
                Width = new GridLength(percentagePerColumn, GridUnitType.Star)
            };
            Root.ColumnDefinitions.Add(columnDef);
        }
        
        const double percentagePerRow = 100.0 / RowCount;
        for (var i = 0; i < RowCount; i++)
        {
            var rowDef = new RowDefinition
            {
                Height = new GridLength(percentagePerRow, GridUnitType.Star)
            };
            Root.RowDefinitions.Add(rowDef);
        }

        await TryLoadSettings();
        _imageControl = new Image
        {
            Source = null,
            Stretch = Stretch.UniformToFill
        };
        ApplySettings();
        
        Grid.SetRowSpan(_imageControl, 9999);
        Grid.SetColumnSpan(_imageControl, 9999);
        Canvas.SetZIndex(_imageControl, -1);
        Root.Children.Add(_imageControl);

        await TryLoadWidgets();
        await AddWidgetsToGrid();

        for (var i = 1; i < ColumnCount; i++)
        {
            var line = new Rectangle
            {
                Width = i == MidwayColumn ? 3 : 1,
                Fill = new SolidColorBrush(Colors.Orange),
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed,
            };
            Grid.SetColumn(line, i);
            Grid.SetRowSpan(line, RowCount);
            Canvas.SetZIndex(line, 1000);
            Root.Children.Add(line);
        }

        for (var i = 1; i < RowCount; i++)
        {
            var line = new Rectangle
            {
                Height = i == MidwayRow ? 3 : 1,
                Fill = new SolidColorBrush(Colors.Orange),
                VerticalAlignment = VerticalAlignment.Top,
                Visibility = Visibility.Collapsed,
            };
            Grid.SetRow(line, i);
            Grid.SetColumnSpan(line, ColumnCount);
            Canvas.SetZIndex(line, 1000);
            Root.Children.Add(line);
        }

        
        _updateTimer = new System.Timers.Timer(50);
        _updateTimer.Elapsed += async (_,_) => await TimerTick();
        _updateTimer.AutoReset = true;
        _updateTimer.Enabled = true;  
        
        _updateTimer.Start();
        
        ApplyTheme();
    }

    private int _imageIndex = -1;
    private Random _random = new();
    private Image _imageControl;
    private Task TimerTick()
    {
        //TODO
        /*if (_settings.DoSlideshow)
        {
            var i = (int)Math.Ceiling((DateTime.Now.Hour * 3600d + DateTime.Now.Minute * 60 + DateTime.Now.Second + (DateTime.Now.Millisecond / 1000d)) / (_settings.DisplayTime <= 0 ? 0.01 : _settings.DisplayTime));
            if (_imageIndex != i)
            {
                _imageIndex = i;
                
                if (!Directory.Exists(_settings.FolderPath))
                {
                    //TODO
                    // MessageBox.Show("The specified directory does not exist for slideshow images, turning off slideshow.",
                    //     "Slideshow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _settings.DoSlideshow = false;
                    return Task.CompletedTask;
                }

                string[] extensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".avi", ".mov", ".wmv"];

                List<string> images = [];
                images = Directory.EnumerateFiles(_settings.FolderPath, "*.*", SearchOption.AllDirectories)
                    .Where(file => extensions.Contains(Path.GetExtension(file).ToLower())).Order().ToList();

                if (images.Count == 0)
                {
                    //TODO
                    // MessageBox.Show($"No images found, turning off slideshow.",
                    //     "Slideshow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _settings.DoSlideshow = false;
                    return Task.CompletedTask;
                }

                AppServer.UiDispatcherQueue.TryEnqueue(() =>
                {
                    Uri? uri = null;
                    try
                    {
                        uri = new Uri(images[_settings.RandomPicking
                            ? _random.Next(0, images.Count)
                            : _imageIndex % images.Count]);
                        
                        var source = new BitmapImage(uri);

                        _imageControl.Source = source;
                        // ImageBehavior.SetAnimatedSource(_imageControl, source); TODO
                    }
                    catch (Exception _)
                    {
                        Console.WriteLine("error: "+uri??"null"); //TODO: look more into this
                    }
                });
            }
        }*/

        return Task.CompletedTask;
    }
    
    public Theme CurrentTheme { get; set { field = value; ApplyTheme(); } } = DefaultThemes.DarkMode;

    private void ApplyTheme()
    {
        foreach (var control in Root.Children)
        {
            if (control is Rectangle line)
                line.Fill = new SolidColorBrush(CurrentTheme.PrimaryBackgroundColorSlightTransparent);
            else if (control is WidgetEditOverlay edit)
                edit.CurrentTheme = CurrentTheme;
            else if (control is WidgetBase widget)
                widget.CurrentTheme = CurrentTheme;
        }
    }

    private void ApplySettings()
    {
        try
        {
            if (!_settings.DoSlideshow)
            {
                var source = new BitmapImage(new Uri(_settings.BackgroundPath));
                _imageControl.Source = source;
                // ImageBehavior.SetAnimatedSource(_imageControl, source); TODO
            }
        }
        catch (IOException _)
        {
            _imageControl.Source = null;
        }
    }

    private async Task<WidgetBase?> GetWidget(string widgetName, Dictionary<string, object>? settings = null, WidgetData? widgetData = null)
    {
        return AvaliableWidgets.TryGetValue(widgetName, out var value) ? await value.Factory(_manager, settings,widgetData ?? new WidgetData { Name = widgetName }) : null;
    }

    private async Task TryLoadWidgets()
    {
        var path = FoxyFileManager.BuildFilePath(WidgetsFileName, FoxyFileManager.FolderType.Widgets, _instanceData.Name);
        if (await FoxyFileManager.ReadFromFileAsync<List<WidgetData>>(path) is { code: not FoxyFileManager.ReturnCode.NotFound, content: not null } result)
        {
            _savedWidgets = result.content;
        }
        else
        {
            _savedWidgets = GetDefaultWidgets();
            await SaveWidgetsToJson();
        }

        List<WidgetData> GetDefaultWidgets() =>
        [
            new()
            {
                Name = WidgetBase.GetWidgetName<ExampleWidget>(),
                Row = 0,
                Column = 0,
                RowSpan = 3,
                ColumnSpan = 3,
                ZIndex = 10,
            },
            //TODO:
            /*new()
            {
                Name = TitleWidget.StaticWidgetName,
                Row = 4,
                Column = 13,
                RowSpan = 5,
                ColumnSpan = 14
            },
            new()
            {
                Name = SearchWidget.StaticWidgetName,
                Row = 8,
                Column = 10,
                RowSpan = 1,
                ColumnSpan = 20
            },
            new()
            {
                Name = EditConfigWidget.StaticWidgetName,
                Row = 1,
                Column = 38,
                RowSpan = 1,
                ColumnSpan = 1
            },
            new()
            {
                Name = TimeWidget.StaticWidgetName,
                Row = 17,
                Column = 34,
                RowSpan = 3,
                ColumnSpan = 5
            }*/
        ];
    }

    private async Task TryLoadSettings()
    {
        var path = FoxyFileManager.BuildFilePath(SettingsFileName, FoxyFileManager.FolderType.Data, _instanceData.Name);
        if (File.Exists(path))
        {
            try
            {
                var jsonData = await File.ReadAllTextAsync(path);
                _settings = JsonSerializer.Deserialize<HomeSettings>(jsonData) ?? GetDefaults();
            }
            catch
            {
                _settings = GetDefaults();
            }
        }
        else
        {
            _settings = GetDefaults();
            await SaveWidgetsToJson();
        }

        HomeSettings GetDefaults() => new() {
            BackgroundPath = "C:\\Users\\penfo\\Downloads\\alone-cyberpunk-morning-4k-xi.jpg"/*TODO move to background on website*/,
        };
    }

    private async Task SaveWidgetsToJson()
    {
        var path = FoxyFileManager.BuildFilePath(WidgetsFileName, FoxyFileManager.FolderType.Widgets, _instanceData.Name);
        await FoxyFileManager.SaveToFileAsync(path, _savedWidgets);
    }

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private async Task SaveSettingsToJson()
    {
        var path = FoxyFileManager.BuildFilePath(SettingsFileName, FoxyFileManager.FolderType.Data, _instanceData.Name);
        await FoxyFileManager.SaveToFileAsync(path, _settings);
    }

    private async Task AddWidgetsToGrid()
    {
        Root.Children.Clear();
        Root.Children.Add(_imageControl);
        await Task.WhenAll(_savedWidgets.Select(AddWidget));
    }

    private async Task AddWidget(WidgetData widgetData)
    {
        var widget = await GetWidget(widgetData.Name, widgetData.Settings, widgetData);
        if (widget == null) return; //TODO
        
        Grid.SetRow(widget, widgetData.Row);
        Grid.SetColumn(widget, widgetData.Column);
        Grid.SetRowSpan(widget, widgetData.RowSpan);
        Grid.SetColumnSpan(widget, widgetData.ColumnSpan);
        Canvas.SetZIndex(widget, widgetData.ZIndex);

        Root.Children.Add(widget);

        if (InEditMode)
        {
            var overlay = new WidgetEditOverlay(widget)
            {
                CurrentTheme = CurrentTheme
            };
            SetupOverlayEvents(overlay);
            Grid.SetColumn(overlay, widget.LayoutData.Column);
            Grid.SetRow(overlay, widget.LayoutData.Row);
            Grid.SetColumnSpan(overlay, widget.LayoutData.ColumnSpan);
            Grid.SetRowSpan(overlay, widget.LayoutData.RowSpan);
            Canvas.SetZIndex(overlay, 1000 + widget.LayoutData.ZIndex);
            Root.Children.Add(overlay);
        }
    }

    private async Task CreateWidget(string IWidgetName)
    {
        var wData = new WidgetData()
        {
            Name = IWidgetName,
            Row = MidwayRow - 2,
            Column = MidwayColumn - 2,
            RowSpan = 4,
            ColumnSpan = 4,
        };

        _savedWidgets.Add(wData);
        await AddWidget(wData);
    }

    internal void EditModeStart()
    {
        if (InEditMode) return;

        ToggleEditMode?.Invoke(true);
        InEditMode = true;

        foreach (var c in Root.Children)
        {
            switch (c)
            {
                case WidgetBase w:
                    var overlay = new WidgetEditOverlay(w)
                    {
                        CurrentTheme = CurrentTheme
                    };
                    SetupOverlayEvents(overlay);
                    Grid.SetColumn(overlay, w.LayoutData.Column);
                    Grid.SetRow(overlay, w.LayoutData.Row);
                    Grid.SetColumnSpan(overlay, w.LayoutData.ColumnSpan);
                    Grid.SetRowSpan(overlay, w.LayoutData.RowSpan);
                    Canvas.SetZIndex(overlay, 1000 + w.LayoutData.ZIndex);
                    Root.Children.Add(overlay);
                    break;
                case Rectangle l:
                    l.Visibility = Visibility.Visible;
                    break;
            }
        }
    }

    internal void EditModeEnd()
    {
        if (!InEditMode) return;

        InEditMode = false;
        ToggleEditMode?.Invoke(false);
        _hoveredOverlay = null;
        RefreshCursor();

        foreach (var c in Root.Children)
        {
            switch (c)
            {
                case WidgetEditOverlay w:
                    Root.Children.Remove(w);
                    break;
                case Rectangle l:
                    l.Visibility = Visibility.Collapsed;
                    break;
            }
        }
    }

    #region OverlayEvents
    private bool _isMouseDown = false;
    private Point? _mouseOffset;
    private string? _currentBorder;
    private bool _blockOverlayButtonEvents = false;

    #region Very ugly widget resize function, but if it works it works.
    private void Root_OnPointerMoved(object s, PointerRoutedEventArgs e)
    {
        if (!_isMouseDown || _hoveredOverlay is null) return;

        var originalLayout = _hoveredOverlay.AttachedWidget.LayoutData;
        var originalBorder = _currentBorder;
        try
        {
            var mousePosition = e.GetCurrentPoint(Root).Position;

            var columnWidth = Root.ActualWidth / ColumnCount;
            var rowHeight = Root.ActualHeight / RowCount;

            if (_currentBorder == "")
            {
                var offsetColumnIndex = Math.Max(
                    Math.Min(
                        (int)Math.Floor((mousePosition.X - _mouseOffset?.X ?? 0) / columnWidth),
                        ColumnCount - _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan),
                    0);
                var offsetRowIndex = Math.Max(
                    Math.Min(
                        (int)Math.Floor((mousePosition.Y - _mouseOffset?.Y ?? 0) / rowHeight),
                        RowCount - _hoveredOverlay.AttachedWidget.LayoutData.RowSpan),
                    0);
                
                Grid.SetColumn(_hoveredOverlay, offsetColumnIndex);
                Grid.SetRow(_hoveredOverlay, offsetRowIndex);
                Grid.SetColumn(_hoveredOverlay.AttachedWidget, offsetColumnIndex);
                Grid.SetRow(_hoveredOverlay.AttachedWidget, offsetRowIndex);

                _hoveredOverlay.AttachedWidget.LayoutData.Column = offsetColumnIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.Row = offsetRowIndex;
                return;
            }
            
            var mouseColumnIndex = Math.Max(
                Math.Min(
                    (int)Math.Floor((mousePosition.X) / columnWidth),
                    ColumnCount),
                0);
            var mouseRowIndex = Math.Max(
                Math.Min(
                    (int)Math.Floor((mousePosition.Y) / rowHeight),
                    RowCount),
                0);
            
            var mouseColumnIndexInclusive = Math.Max(
                Math.Min(
                    (int)Math.Ceiling((mousePosition.X) / columnWidth),
                    ColumnCount),
                0);
            var mouseRowIndexInclusive = Math.Max(
                Math.Min(
                    (int)Math.Ceiling((mousePosition.Y) / rowHeight),
                    RowCount),
                0);
            
            if (_currentBorder == "T")
            {
                if (mouseRowIndex < _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan)
                    goto borderT;
                else
                {
                    _currentBorder = "B";

                    var newRow = _hoveredOverlay.AttachedWidget.LayoutData.Row +
                                 _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                    
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, newRow);
                    Grid.SetRow(_hoveredOverlay, newRow);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = newRow;

                    goto borderB;
                }
            }
            else if (_currentBorder == "B")
            {
                if (mouseRowIndexInclusive >= _hoveredOverlay.AttachedWidget.LayoutData.Row)
                    goto borderB;
                else
                {
                    _currentBorder = "T";
                    
                    var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex;
                    
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                    Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                    Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                    Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                    
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                }
            }
            else if (_currentBorder == "L")
            {
                if (mouseColumnIndex < _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan)
                    goto borderL;
                else
                {
                    _currentBorder = "R";

                    var newColumn = _hoveredOverlay.AttachedWidget.LayoutData.Column +
                                   _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
                    
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, newColumn);
                    Grid.SetColumn(_hoveredOverlay, newColumn);
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = newColumn;

                    goto borderR;
                }
            }
            else if (_currentBorder == "R")
            {
                if (mouseColumnIndexInclusive >= _hoveredOverlay.AttachedWidget.LayoutData.Column)
                    goto borderR;
                else
                {
                    _currentBorder = "L";
                    
                    var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex;
                    
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                    Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                    Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                    Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                    
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                }
            }
            else if (_currentBorder == "TL")
            {
                var atRowBoundary = mouseRowIndex >= _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                var atColumnBoundary = mouseColumnIndex >= _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
            
                if (!atRowBoundary && !atColumnBoundary) 
                    goto borderTL;
                else if (atRowBoundary && !atColumnBoundary) 
                { 
                    _currentBorder = "BL"; 
                    RefreshCursor();
                    var newRow = _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, newRow);
                    Grid.SetRow(_hoveredOverlay, newRow);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = newRow;
                    goto borderBL; 
                }
                else if (!atRowBoundary && atColumnBoundary) 
                { 
                    _currentBorder = "TR"; 
                    RefreshCursor();
                    var newColumn = _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, newColumn);
                    Grid.SetColumn(_hoveredOverlay, newColumn);
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = newColumn;
                    goto borderTR; 
                }
                else 
                { 
                    _currentBorder = "BR"; 
                    var newRow = _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                    var newColumn = _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, newRow);
                    Grid.SetRow(_hoveredOverlay, newRow);
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, newColumn);
                    Grid.SetColumn(_hoveredOverlay, newColumn);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = newRow;
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = newColumn;
                    goto borderBR; 
                }
            }
            else if (_currentBorder == "TR")
            {
                var atRowBoundary = mouseRowIndex >= _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                var atColumnBoundary = mouseColumnIndexInclusive <= _hoveredOverlay.AttachedWidget.LayoutData.Column;
            
                if (!atRowBoundary && !atColumnBoundary) 
                    goto borderTR;
                else if (atRowBoundary && !atColumnBoundary) 
                { 
                    _currentBorder = "BR"; 
                    RefreshCursor();
                    var newRow = _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, newRow);
                    Grid.SetRow(_hoveredOverlay, newRow);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = newRow;
                    goto borderBR; 
                }
                else if (!atRowBoundary && atColumnBoundary) 
                { 
                    _currentBorder = "TL"; 
                    RefreshCursor();
                    var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex;
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                    Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                    Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                    Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                    goto borderTL; 
                }
                else 
                { 
                    _currentBorder = "BL"; 
                    var newRow = _hoveredOverlay.AttachedWidget.LayoutData.Row + _hoveredOverlay.AttachedWidget.LayoutData.RowSpan;
                    var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, newRow);
                    Grid.SetRow(_hoveredOverlay, newRow);
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                    Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                    Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                    Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = newRow;
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                    goto borderBL; 
                }
            }
            else if (_currentBorder == "BL")
            {
                var atRowBoundary = mouseRowIndexInclusive <= _hoveredOverlay.AttachedWidget.LayoutData.Row;
                var atColumnBoundary = mouseColumnIndex >= _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
            
                if (!atRowBoundary && !atColumnBoundary) 
                    goto borderBL;
                else if (atRowBoundary && !atColumnBoundary) 
                { 
                    _currentBorder = "TL"; 
                    RefreshCursor();
                    var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                    Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                    Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                    Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                    goto borderTL; 
                }
                else if (!atRowBoundary && atColumnBoundary) 
                { 
                    _currentBorder = "BR"; 
                    RefreshCursor();
                    var newColumn = _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, newColumn);
                    Grid.SetColumn(_hoveredOverlay, newColumn);
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = newColumn;
                    goto borderBR; 
                }
                else 
                { 
                    _currentBorder = "TR"; 
                    var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex;
                    var newColumn = _hoveredOverlay.AttachedWidget.LayoutData.Column + _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                    Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, newColumn);
                    Grid.SetColumn(_hoveredOverlay, newColumn);
                    Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                    Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = newColumn;
                    _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                    goto borderTR; 
                }
            }
            else if (_currentBorder == "BR")
            {
                var atRowBoundary = mouseRowIndexInclusive <= _hoveredOverlay.AttachedWidget.LayoutData.Row;
                var atColumnBoundary = mouseColumnIndexInclusive <= _hoveredOverlay.AttachedWidget.LayoutData.Column;
            
                if (!atRowBoundary && !atColumnBoundary) 
                    goto borderBR;
                else if (atRowBoundary && !atColumnBoundary) 
                { 
                    _currentBorder = "TR"; 
                    RefreshCursor();
                    var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                    Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                    Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                    Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                    goto borderTR; 
                }
                else if (!atRowBoundary && atColumnBoundary) 
                { 
                    _currentBorder = "BL"; 
                    RefreshCursor();
                    var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex;
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                    Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                    Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                    Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                    goto borderBL; 
                }
                else 
                { 
                    _currentBorder = "TL"; 
                    var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex;
                    var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex;
                    Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                    Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                    Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                    Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                    Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                    Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                    Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                    Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                    _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                    _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                    _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                    goto borderTL; 
                }
            }

            
            return;

            #region SimpleBorderCases
            borderT:
            {
                var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.RowSpan +
                                 (_hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex);

                Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                return;
            }
            borderB:
            {
                var newRowSpan = mouseRowIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Row;
                if (newRowSpan <= 0) return;
                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                return;
            }
            borderL:
            {
                var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan +
                                   (_hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex);

                Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            borderR:
            {
                var newColumnSpan = mouseColumnIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Column;
                if (newColumnSpan <= 0) return;
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            borderTL:
            {
                var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.RowSpan +
                                 (_hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex);
                var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan +
                                   (_hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex);

                Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            borderTR:
            {
                var newRowSpan = _hoveredOverlay.AttachedWidget.LayoutData.RowSpan +
                                 (_hoveredOverlay.AttachedWidget.LayoutData.Row - mouseRowIndex);
                var newColumnSpan = mouseColumnIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Column;
                if (newColumnSpan <= 0) return;

                Grid.SetRow(_hoveredOverlay.AttachedWidget, mouseRowIndex);
                Grid.SetRow(_hoveredOverlay, mouseRowIndex);
                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.Row = mouseRowIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            
            borderBL:
            {
                var newRowSpan = mouseRowIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Row;
                var newColumnSpan = _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan +
                                   (_hoveredOverlay.AttachedWidget.LayoutData.Column - mouseColumnIndex);
                if (newRowSpan <= 0) return;

                Grid.SetColumn(_hoveredOverlay.AttachedWidget, mouseColumnIndex);
                Grid.SetColumn(_hoveredOverlay, mouseColumnIndex);
                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.Column = mouseColumnIndex;
                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            borderBR:
            {
                var newRowSpan = mouseRowIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Row;
                var newColumnSpan = mouseColumnIndexInclusive - _hoveredOverlay.AttachedWidget.LayoutData.Column;
                if (newRowSpan <= 0 || newColumnSpan <= 0) return;

                Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, newRowSpan);
                Grid.SetRowSpan(_hoveredOverlay, newRowSpan);
                Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, newColumnSpan);
                Grid.SetColumnSpan(_hoveredOverlay, newColumnSpan);

                _hoveredOverlay.AttachedWidget.LayoutData.RowSpan = newRowSpan;
                _hoveredOverlay.AttachedWidget.LayoutData.ColumnSpan = newColumnSpan;
                return;
            }
            #endregion
        }
        catch (Exception exception)
        {
            //TODO: log error
            // there are errors, but might not matter too much
            
            // just reset
            _currentBorder = originalBorder;
            RefreshCursor();

            _hoveredOverlay.AttachedWidget.LayoutData = originalLayout;
            Grid.SetRowSpan(_hoveredOverlay.AttachedWidget, originalLayout.RowSpan);
            Grid.SetRowSpan(_hoveredOverlay, originalLayout.RowSpan);
            Grid.SetColumnSpan(_hoveredOverlay.AttachedWidget, originalLayout.ColumnSpan);
            Grid.SetColumnSpan(_hoveredOverlay, originalLayout.ColumnSpan);
            Grid.SetRow(_hoveredOverlay.AttachedWidget, originalLayout.Row);
            Grid.SetRow(_hoveredOverlay, originalLayout.Row);
            Grid.SetColumn(_hoveredOverlay.AttachedWidget, originalLayout.Column);
            Grid.SetColumn(_hoveredOverlay, originalLayout.Column);
        }
    }
    #endregion
    
    private void Root_OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isMouseDown = false;
        _mouseOffset = null;
        _currentBorder = null;
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += (sender, args) =>
        {
            timer.Stop();
            _blockOverlayButtonEvents = false;
        };
        timer.Start();
        
        RefreshCursor();
    }
    
    private void SetupOverlayEvents(WidgetEditOverlay overlay)
    {
        overlay.PointerRefreshRequested += PointerRefreshRequested;
        overlay.RootEntered += RootEntered;
        overlay.RootExited += RootExited;
        overlay.MouseUp += MouseUp;
        overlay.MouseDown += MouseDown;
        overlay.SettingsClicked += SettingsClicked;
        overlay.DeleteClicked += DeleteClicked;
    }
    
    private void DeleteClicked(WidgetEditOverlay s)
    {
        if (_blockOverlayButtonEvents) return;
        
        _savedWidgets.Remove(s.AttachedWidget.LayoutData);
        Root.Children.Remove(s.AttachedWidget);
        Root.Children.Remove(s);
        
        if (_hoveredOverlay == s) _hoveredOverlay = null;
    }

    private void SettingsClicked(WidgetEditOverlay s)
    {
        if (_blockOverlayButtonEvents) return;
        
        // throw new NotImplementedException();
    }

    private void MouseDown(WidgetEditOverlay s, PointerRoutedEventArgs e)
    {
        if (_hoveredOverlay != s) return;
        
        _isMouseDown = true;
        _blockOverlayButtonEvents = true;
        
        var gridPoint = new Point(_hoveredOverlay.AttachedWidget.LayoutData.Column * (Root.ActualWidth / ColumnCount), _hoveredOverlay.AttachedWidget.LayoutData.Row * (Root.ActualHeight / RowCount));
        var mousePoint = e.GetCurrentPoint(Root).Position;
        _mouseOffset = new Point(mousePoint.X - gridPoint.X, mousePoint.Y - gridPoint.Y);
        
        _currentBorder = _hoveredOverlay.CurrentBorder;
    }

    private void MouseUp(WidgetEditOverlay s)
    {
        if (_hoveredOverlay != s) return;
        
        _isMouseDown = false;
        _mouseOffset = null;
        _currentBorder = null;
        var timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += (sender, args) =>
        {
            timer.Stop();
            _blockOverlayButtonEvents = false;
        };
        timer.Start();
    }

    private void RootExited(WidgetEditOverlay s)
    {
        if (_isMouseDown) return;
        if (_hoveredOverlay == s) _hoveredOverlay = null;
        RefreshCursor();
    }

    private void RootEntered(WidgetEditOverlay s)
    {
        if (_isMouseDown) return;
        if (_hoveredOverlay is null) _hoveredOverlay = s;
        else if (_hoveredOverlay.AttachedWidget.LayoutData.ZIndex > s.AttachedWidget.LayoutData.ZIndex) _hoveredOverlay = s;
        RefreshCursor();
    }

    private void PointerRefreshRequested(WidgetEditOverlay s)
    {
        if (_isMouseDown) return;
        if (s != _hoveredOverlay) return;
        RefreshCursor();
    }

    private void RefreshCursor()
    {
        if (_hoveredOverlay is null)
        {
            ProtectedCursor = _defaultCursor;
        }
        else
        {
            ProtectedCursor = (_currentBorder ?? _hoveredOverlay.CurrentBorder) switch
            {
                "" => _moveCursor,
                "T" => _sizeNSCursor,
                "B" => _sizeNSCursor,
                "L" => _sizeWECursor,
                "R" => _sizeWECursor,
                "TL" => _sizeNwSeCursor,
                "TR" => _sizeNeSwCursor,
                "BL" => _sizeNeSwCursor,
                "BR" => _sizeNwSeCursor,
                _ => _defaultCursor,
            };
        }
    }
    
    private readonly InputCursor _defaultCursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.Arrow, 0));
    private readonly InputCursor _moveCursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.SizeAll, 0));
    private readonly InputCursor _sizeWECursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.SizeWestEast, 0));
    private readonly InputCursor _sizeNSCursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.SizeNorthSouth, 0));
    private readonly InputCursor _sizeNeSwCursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.SizeNortheastSouthwest, 0));
    private readonly InputCursor _sizeNwSeCursor = InputCursor.CreateFromCoreCursor(new(CoreCursorType.SizeNorthwestSoutheast, 0));
    #endregion
    

    public (string name, MaterialIconKind icon, WidgetCategory category)[] GetWidgetOptions()
    {
        return AvaliableWidgets
            .Select(pair => (pair.Key, pair.Value.Icon, pair.Value.Category))
            .ToArray();
    }

    public (MaterialIconKind icon, OptionType type, string name)[] GetHomeOptions()
    {
        return
        [
            (MaterialIconKind.ContentSave, OptionType.Save, "Save"),
            (MaterialIconKind.ContentSaveMove, OptionType.SaveExit, "Save and Exit"),
            (MaterialIconKind.Logout, OptionType.Exit, "Exit Without Saving"), 
            (MaterialIconKind.Image, OptionType.ChangeImage, "Change Background Image"),
            (MaterialIconKind.FolderImage, OptionType.ChangeSlideshow, "Change Slideshow Background"),
        ];
    }

    public async Task OptionClicked(OptionType type)
    {
        switch (type)
        {
            case OptionType.Save:
                await Task.WhenAll(SaveWidgetsToJson(), SaveSettingsToJson());
                break;
            case OptionType.SaveExit:
                await Task.WhenAll(SaveWidgetsToJson(), SaveSettingsToJson());
                EditModeEnd();
                break;
            case OptionType.Exit:
                await Task.WhenAll(TryLoadWidgets(), TryLoadSettings());
                await AddWidgetsToGrid();
                ApplySettings();
                EditModeEnd();
                break;
            case OptionType.ChangeImage:
                throw new NotImplementedException(); // can be a url, so show a popup with a file picker setting
                /*var dialog = new OpenFileDialog
                {
                    Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;|All Files (*.*)|*.*",
                    Title = "Pick a background image",
                    Multiselect = false
                };

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    _settings.BackgroundPath = dialog.FileName;
                    ApplySettings();
                }*/
                break;
            case OptionType.ChangeSlideshow:
                throw new NotImplementedException();
                /*var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            
                if (adornerLayer != null)
                {
                    var settingsAdorner = new SettingsAdorner(SlideshowSettings, $"Slideshow Settings", this);
                    settingsAdorner.CloseRequested += (returnSettings) =>
                    {
                        adornerLayer.Remove(settingsAdorner);
                        if (returnSettings.TryGetValue(0, out var valueRaw1) && valueRaw1.setting is BoolSetting boolSetting1)
                            _settings.DoSlideshow = boolSetting1.Value;
                        if (returnSettings.TryGetValue(1, out var valueRaw2) && valueRaw2.setting is BoolSetting boolSetting2)
                            _settings.RandomPicking = boolSetting2.Value;
                        if (returnSettings.TryGetValue(2, out var valueRaw3) && valueRaw3.setting is DecimalSetting intSetting)
                            _settings.DisplayTime = intSetting.Value;
                        if (returnSettings.TryGetValue(3, out var valueRaw4) && valueRaw4.setting is FolderPickerSetting folderSetting)
                            _settings.FolderPath = folderSetting.Value;
                        ApplySettings();
                    };
                    adornerLayer.Add(settingsAdorner);
                }*/
                
                break;
        }
    }

    private Dictionary<int, ISetting> SlideshowSettings => new()
    {
        [0] = new BoolSetting("Enable Slideshow", "", _settings.DoSlideshow),
        [1] = new BoolSetting("Pick Random Images (will pick in alphabetical order when off)", "", _settings.RandomPicking),
        [2] = new DecimalSetting("Display Interval (in seconds)", "", _settings.DisplayTime),
        [3] = new FolderPickerSetting("FolderPath (can have nested folders)", "", _settings.FolderPath),
    };
    
    public async Task AddWidgetClicked(string name)
    {
        await CreateWidget(name);
    }

    public enum OptionType
    {
        Save,
        SaveExit,
        Exit,
        ChangeImage,
        ChangeSlideshow,
    }
}

public record WidgetData
{
    public required string Name { get; init; }
    public int Row { get; set; }
    public int Column { get; set; }
    public int RowSpan { get; set; }
    public int ColumnSpan { get; set; }
    public int ZIndex { get; set; } = 1;
    public Dictionary<string, object>? Settings { get; set; }
}

internal record HomeSettings
{
    // background
    public required string BackgroundPath { get; set; }
    
    // slideshow background
    public bool DoSlideshow { get; set; }
    public bool RandomPicking { get; set; }
    public string FolderPath { get; set; } = "";
    public decimal DisplayTime { get; set; } = 60;
}