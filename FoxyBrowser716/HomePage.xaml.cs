using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using FoxyBrowser716.HomeWidgets;

namespace FoxyBrowser716;

public partial class HomePage : UserControl
{
	private const string WidgetsFileName = "widgets.json";
	private List<WidgetData> _savedWidgets;
	
	public HomePage()
	{
		InitializeComponent();
	}

	private async Task TryLoadWidgets()
	{
		if (File.Exists(WidgetsFileName))
		{
			try
			{
				var jsonData = await File.ReadAllTextAsync(WidgetsFileName);
				_savedWidgets = JsonSerializer.Deserialize<List<WidgetData>>(jsonData) ?? GetDefaultWidgets();
			}
			catch
			{
				_savedWidgets = GetDefaultWidgets();
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
			}
		];
	}

	private async Task SaveWidgetsToJson()
	{
		try
		{
			var jsonData = JsonSerializer.Serialize(_savedWidgets, new JsonSerializerOptions { WriteIndented = true });
			await File.WriteAllTextAsync(WidgetsFileName, jsonData);
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
			
			// do not await each task at one time,
			// just add the task to a list and await all of them at one time
			initTasks.Add(widget.Initialize(manager));
			
			Grid.SetRow(widget, widgetData.Row);
			Grid.SetColumn(widget, widgetData.Column);
			Grid.SetRowSpan(widget, widgetData.RowSpan);
			Grid.SetColumnSpan(widget, widgetData.ColumnSpan);

			MainGrid.Children.Add(widget);
		}
		
		await Task.WhenAll(initTasks);
	}

	private static IWidget? GetWidget(string widgetName)
	{
		return widgetName switch
		{
			SearchWidget.StaticWidgetName => new SearchWidget(),
			TitleWidget.StaticWidgetName => new TitleWidget(),
			YoutubeWidget.StaticWidgetName => new YoutubeWidget(),
			TimeWidget.StaticWidgetName => new TimeWidget(),
			_ => null
		};
	}

	public async Task Initialize(TabManager manager)
	{
		await TryLoadWidgets();
		await AddWidgetsToGrid(manager);
	}
}

public record WidgetData
{
	public required string Name { get; set; }
	public int Row { get; set; }
	public int Column { get; set; }
	public int RowSpan { get; set; }
	public int ColumnSpan { get; set; }
}