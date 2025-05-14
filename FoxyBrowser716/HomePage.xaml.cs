using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FoxyBrowser716.Converters;
using FoxyBrowser716.HomeWidgets;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using Material.Icons;
using Material.Icons.WPF;
using Microsoft.Win32;
using WpfAnimatedGif;

namespace FoxyBrowser716;

public partial class HomePage : UserControl
{
    private const string WidgetsFileName = "homeWidgets.json";
    private const string SettingsFileName = "homeSettings.json";
    private const string DefaultBackgroundName = "FoxyBrowserDefaultBackground.jpg";
    private List<WidgetData> _savedWidgets;
    private HomeSettings _settings;
    private TabManager _manager;
    private InstanceManager _instanceData;
    
    public event Action<bool> ToggleEditMode;
    public bool InEditMode;

    private static Dictionary<string, Func<IWidget>> WidgetOptions = new()
    {
        { SearchWidget.StaticWidgetName, () => new SearchWidget() },
        { TitleWidget.StaticWidgetName, () => new TitleWidget() },
        { TimeWidget.StaticWidgetName, () => new TimeWidget() },
        { DateWidget.StaticWidgetName, () => new DateWidget() },
        { EditConfigWidget.StaticWidgetName, () => new EditConfigWidget() },
        { YoutubeWidget.StaticWidgetName, () => new YoutubeWidget() },
    };

    private System.Timers.Timer updateTimer;
    
    public HomePage()
    {
        InitializeComponent();
    }

    public async Task Initialize(TabManager manager, InstanceManager instanceManager)
    {
        _manager = manager; // save manager for later use
        _instanceData = instanceManager;
        
        await TryLoadSettings();
        _imageControl = new Image
        {
            Source = null,
            Stretch = Stretch.UniformToFill
        };
        ApplySettings();
        Panel.SetZIndex(_imageControl, -1);
        MainGrid.Children.Add(_imageControl);

        await TryLoadWidgets();
        await AddWidgetsToGrid();


        updateTimer = new System.Timers.Timer(250);
        updateTimer.Elapsed += async (_,_) => await TimerTick();
        updateTimer.AutoReset = true;
        updateTimer.Enabled = true;  
        
        updateTimer.Start();
    }

    private int _imageIndex = -1;
    private Random _random = new();
    private Image _imageControl;
    private async Task TimerTick()
    {
        if (_settings.DoSlideshow)
        {
            var i = (DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second) / _settings.DisplayTime;
            if (_imageIndex != i)
            {
                _imageIndex = i;
                
                if (!Directory.Exists(_settings.FolderPath))
                {
                    MessageBox.Show("The specified directory does not exist for slideshow images, turning off slideshow.",
                        "Slideshow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _settings.DoSlideshow = false;
                    return;
                }

                string[] extensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".avi", ".mov", ".wmv"];

                List<string> images = [];
                try
                {
                    images = Directory.EnumerateFiles(_settings.FolderPath, "*.*", SearchOption.AllDirectories)
                        .Where(file => extensions.Contains(Path.GetExtension(file).ToLower())).Order().ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while reading slideshow images: {ex.Message}",
                        "Slideshow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (images.Count == 0)
                {
                    MessageBox.Show($"No images found, turning off slideshow.",
                        "Slideshow Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _settings.DoSlideshow = false;
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    Uri? uri = null;
                    try
                    {
                        uri = new Uri(images[_settings.RandomPicking
                            ? _random.Next(0, images.Count)
                            : _imageIndex % images.Count]);
                        
                        var source = new BitmapImage(uri);

                        _imageControl.Source = source;
                        ImageBehavior.SetAnimatedSource(_imageControl, source);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("e: "+uri??"null");
                    }
                });
            }
        }
    }

    private void ApplySettings()
    {
        if (!_settings.DoSlideshow)
        {
            var source = new BitmapImage(new Uri(_settings.BackgroundPath));
            _imageControl.Source = source;
            ImageBehavior.SetAnimatedSource(_imageControl, source);
            
            Grid.SetRowSpan(_imageControl, 9999);
            Grid.SetColumnSpan(_imageControl, 9999);
            Panel.SetZIndex(_imageControl, -1);
        }
    }

    private static IWidget? GetWidget(string widgetName)
    {
        return WidgetOptions.TryGetValue(widgetName, out var factory) ? factory() : null;
    }

    private async Task TryLoadWidgets()
    {
        var path = Path.Combine(_instanceData.InstanceFolder, WidgetsFileName);
        if (File.Exists(path))
        {
            try
            {
                var jsonData = await File.ReadAllTextAsync(path);
                _savedWidgets = JsonSerializer.Deserialize<List<WidgetData>>(jsonData) ?? GetDefaultWidgets();
            }
            catch
            {
                _savedWidgets = GetDefaultWidgets();
                await SaveSettingsToJson();
            }
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
            }
        ];
    }

    private async Task TryLoadSettings()
    {
        var path = Path.Combine(_instanceData.InstanceFolder, SettingsFileName);
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
            BackgroundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultBackgroundName),
        };
    }

    private async Task SaveWidgetsToJson()
    {
        var path = Path.Combine(_instanceData.InstanceFolder, WidgetsFileName);
        try
        {
            var jsonData = JsonSerializer.Serialize(_savedWidgets, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, jsonData);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Widget Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task SaveSettingsToJson()
    {
        var path = Path.Combine(_instanceData.InstanceFolder, SettingsFileName);
        try
        {
            var jsonData = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, jsonData);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Setting Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task AddWidgetsToGrid()
    {
        MainGrid.Children.Clear();
        MainGrid.Children.Add(_imageControl);
        await Task.WhenAll(_savedWidgets.Select(AddWidget));
    }

    private async Task AddWidget(WidgetData widgetData)
    {
        var widget = GetWidget(widgetData.Name);
        switch (widget)
        {
            case null:
                MessageBox.Show($"Widget {widgetData.Name} not found", "Widget Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            case EditConfigWidget editWidget:
                editWidget.Clicked += EditModeStart;
                Panel.SetZIndex(editWidget, 500);
                break;
        }

        widget.Data = widgetData;

        Grid.SetRow(widget, widgetData.Row);
        Grid.SetColumn(widget, widgetData.Column);
        Grid.SetRowSpan(widget, widgetData.RowSpan);
        Grid.SetColumnSpan(widget, widgetData.ColumnSpan);
        

        MainGrid.Children.Add(widget);

        await widget.Initialize(_manager, widgetData.Settings);
        widget.RemoveRequested += () =>
        {
            _savedWidgets.Remove(widgetData);
            MainGrid.Children.Remove(widget);
        };
        widget.OpenWidgetSettings += wSettings =>
        {
            // var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            //
            // if (adornerLayer != null)
            // {
            //     var settingsAdorner = new SettingsAdorner(wSettings, $"{widget.Name} Settings", this);
            //     settingsAdorner.CloseRequested += () => { adornerLayer.Remove(settingsAdorner); };
            //     adornerLayer.Add(settingsAdorner);
            // }
        };
        
        if (InEditMode)
            widget.InWidgetEditingMode = true;
    }

    private async Task CreateWidget(string widgetName)
    {
        var wData = new WidgetData()
        {
            Name = widgetName,
            Row = 0,
            Column = 0,
            RowSpan = 1,
            ColumnSpan = 1,
        };

        _savedWidgets.Add(wData);
        await AddWidget(wData);
    }

    private void EditModeStart()
    {
        if (InEditMode) return;

        ToggleEditMode?.Invoke(true);
        InEditMode = true;

        foreach (var c in MainGrid.Children)
        {
            if (c is not IWidget w) continue;
            w.InWidgetEditingMode = true;
        }
    }

    private void EditModeEnd()
    {
        if (!InEditMode) return;

        InEditMode = false;
        ToggleEditMode?.Invoke(false);

        foreach (var c in MainGrid.Children)
        {
            if (c is not IWidget w) continue;
            w.InWidgetEditingMode = false;
        }
    }

    public (BitmapSource preview, string name)[] GetWidgetOptions()
    {
        return WidgetOptions.Values
            .Select(v => v())
            .Where(v => v.CanAdd)
            .Select(v => (v.PreviewControl(), v.WidgetName))
            .ToArray();
    }

    public (MaterialIcon? icon, OptionType type, string name)[] GetHomeOptions()
    {
        return
        [
            (new MaterialIcon { Kind = MaterialIconKind.ContentSave, Foreground = Brushes.White}, OptionType.Save, "Save"),
            (new MaterialIcon { Kind = MaterialIconKind.Logout, Foreground = Brushes.White}, OptionType.SaveExit, "Save and Exit"),
            (new MaterialIcon { Kind = MaterialIconKind.Logout, Foreground = Brushes.White}, OptionType.Exit, "Exit Without Saving"),
            (new MaterialIcon { Kind = MaterialIconKind.Image, Foreground = Brushes.White}, OptionType.ChangeImage, "Change Background Image"),
            (new MaterialIcon { Kind = MaterialIconKind.FolderImage, Foreground = Brushes.White}, OptionType.ChangeSlideshow, "Change Slideshow Background"),
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
                var dialog = new OpenFileDialog
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
                }
                break;
            case OptionType.ChangeSlideshow:
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
            
                if (adornerLayer != null)
                {
                    var settingsAdorner = new SettingsAdorner(_slideshowSettings, $"Slideshow Settings", this);
                    settingsAdorner.CloseRequested += (returnSettings) =>
                    {
                        adornerLayer.Remove(settingsAdorner);
                        if (returnSettings.TryGetValue(0, out var valueRaw1) && valueRaw1.setting is WidgetSettingBool boolSetting1)
                            _settings.DoSlideshow = boolSetting1.Value;
                        if (returnSettings.TryGetValue(1, out var valueRaw2) && valueRaw2.setting is WidgetSettingBool boolSetting2)
                            _settings.RandomPicking = boolSetting2.Value;
                        if (returnSettings.TryGetValue(2, out var valueRaw3) && valueRaw3.setting is WidgetSettingInt intSetting)
                            _settings.DisplayTime = intSetting.Value;
                        if (returnSettings.TryGetValue(3, out var valueRaw4) && valueRaw4.setting is WidgetSettingFolderPicker folderSetting)
                            _settings.FolderPath = folderSetting.Value;
                        ApplySettings();
                    };
                    adornerLayer.Add(settingsAdorner);
                }
                
                break;
        }
    }

    private Dictionary<int, (IWidgetSetting setting, string title)> _slideshowSettings => new()
    {
        // [-3] = (new WidgetSettingSubTitle("Hello There"),""),
        // [-2] = (new WidgetSettingDescription("Hello there."),""),
        // [-1] = (new WidgetSettingDiv(),""),
        [0] = (new WidgetSettingBool(_settings.DoSlideshow), "Enable Slideshow"),
        [1] = (new WidgetSettingBool(_settings.RandomPicking), "Pick Random Images (will pick in alphabetical order when off)"),
        [2] = (new WidgetSettingInt(_settings.DisplayTime), "Display Interval (in seconds)"),
        [3] = (new WidgetSettingFolderPicker(_settings.FolderPath), "FolderPath (can have nested folders)"),
    };
    
    public async void AddWidgetClicked(string name)
    {
        try
        {
            await CreateWidget(name);
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Widget Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
    public Dictionary<string, IWidgetSetting>? Settings { get; set; }
}

internal record HomeSettings
{
    // background
    public required string BackgroundPath { get; set; }
    
    // slideshow background
    public bool DoSlideshow { get; set; }
    public bool RandomPicking { get; set; }
    public string FolderPath { get; set; } = "";
    public int DisplayTime { get; set; } = 60;
}