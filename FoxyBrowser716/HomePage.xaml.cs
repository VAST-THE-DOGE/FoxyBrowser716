using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FoxyBrowser716.Converters;
using FoxyBrowser716.HomeWidgets;
using FoxyBrowser716.HomeWidgets.WidgetSettings;
using Material.Icons;
using Material.Icons.WPF;
using Microsoft.Win32;

namespace FoxyBrowser716;

public partial class HomePage : UserControl
{
	private const string WidgetsFileName = "homeWidgets.json";
	private const string SettingsFileName = "homeSettings.json";
	private const string DefaultBackgroundName = "FoxyBrowserDefaultBackground.jpg";
	private List<WidgetData> _savedWidgets;
	private HomeSettings _settings;
	
	public event Action<bool> ToggleEditMode;
	public bool InEditMode;

	private static Dictionary<string, Func<IWidget>> WidgetOptions = new()
	{
		{ SearchWidget.StaticWidgetName, () => new SearchWidget() },
		{ TitleWidget.StaticWidgetName, () => new TitleWidget() },
		{ TimeWidget.StaticWidgetName, () => new TimeWidget() },
		{ EditConfigWidget.StaticWidgetName, () => new EditConfigWidget() },
		{ YoutubeWidget.StaticWidgetName, () => new YoutubeWidget() },
	};

	public HomePage()
	{
		InitializeComponent();
	}
	
	public async Task Initialize(TabManager manager)
	{
		await TryLoadWidgets();
		await TryLoadSettings();
		await AddWidgetsToGrid(manager);
		
		MainGrid.Background = new ImageBrush
		{
			ImageSource = new BitmapImage(new Uri(_settings.BackgroundPath)),
			Stretch = Stretch.UniformToFill
		};
	}
	
	private static IWidget? GetWidget(string widgetName)
	{
		return WidgetOptions.TryGetValue(widgetName, out var factory) ? factory() : null;
	}

	private async Task TryLoadSettings()
	{
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
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
	
	private async Task TryLoadWidgets()
	{
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WidgetsFileName);
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
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, WidgetsFileName);
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
		var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
		try
		{
			//TODO: do this recommendation (reuse the Serializer):
			var jsonData = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(path, jsonData);
		}
		catch (Exception e)
		{
			MessageBox.Show(e.Message, "Widget Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private async Task AddWidgetsToGrid(TabManager manager)
	{
		List<Task> initTasks = [];
		foreach (var widgetData in _savedWidgets)
		{
			var widget = GetWidget(widgetData.Name);
			if (widget is null)
			{
				MessageBox.Show($"Widget {widgetData.Name} not found", "Widget Error", MessageBoxButton.OK, MessageBoxImage.Error);
				continue;
			}

			if (widget is EditConfigWidget editWidget)
			{
				editWidget.Clicked += EditModeStart;
			}
			
			// do not await each task at one time,
			// just add the task to a list and await all of them at one time
			initTasks.Add(widget.Initialize(manager, widgetData.Settings));
			
			Grid.SetRow(widget, widgetData.Row);
			Grid.SetColumn(widget, widgetData.Column);
			Grid.SetRowSpan(widget, widgetData.RowSpan);
			Grid.SetColumnSpan(widget, widgetData.ColumnSpan);

			MainGrid.Children.Add(widget);
		}
		
		await Task.WhenAll(initTasks);
	}

	private void EditModeStart()
	{
		if (InEditMode) return;
		
		ToggleEditMode?.Invoke(true);
		InEditMode = true;
	}
	
	private void EditModeEnd()
	{
		if (!InEditMode) return;
		
		InEditMode = false;
		ToggleEditMode?.Invoke(false);
	}

	public (BitmapSource preview, string name)[] GetWidgetOptions()
	{
		return WidgetOptions.Values
			.Select(v => v())
			.Where(v => v.CanAdd)
			.Select(v => (v.PreviewControl(),v.WidgetName))
			.ToArray();

	}
	
	public (MaterialIcon? icon, OptionType type, string name)[] GetHomeOptions()
	{
		return 
		[
			(new MaterialIcon { Kind = MaterialIconKind.ContentSave, Foreground = Brushes.White},
				OptionType.Save, "Save"),
			(new MaterialIcon { Kind = MaterialIconKind.Logout, Foreground = Brushes.White},
				OptionType.SaveExit, "Save and Exit"),
			(new MaterialIcon { Kind = MaterialIconKind.Logout, Foreground = Brushes.White},
				OptionType.Exit, "Exit Without Saving"),
			(new MaterialIcon { Kind = MaterialIconKind.Image, Foreground = Brushes.White},
				OptionType.ChangeImage, "Change Background Image")
		];
	}

	public async Task OptionClicked(OptionType type)
	{
		switch (type)
		{
			case OptionType.Save:
				break;
			case OptionType.SaveExit:
				break;
			case OptionType.Exit:
				EditModeEnd();
				break;
			case OptionType.ChangeImage:
				var dialog = new OpenFileDialog
				{
					Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*",
					Title = "Pick a background image",
					Multiselect = false
				};

				var result = dialog.ShowDialog();

				if (result == true)
				{
					Console.WriteLine("setting path");
					_settings.BackgroundPath = dialog.FileName;
					MainGrid.Background = new ImageBrush
					{
						ImageSource = new BitmapImage(new Uri(_settings.BackgroundPath)),
						Stretch = Stretch.UniformToFill
					};
				}
				await SaveSettingsToJson();
				break;
		}
	}

	public void AddWidget(string name)
	{
		
	}
	
	public enum OptionType
	{
		Save,
		SaveExit,
		Exit,
		
		ChangeImage
	}
	
	
}

internal record WidgetData
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
	internal required string BackgroundPath { get; set; }
}