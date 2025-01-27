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
	public event Action<string> OnSearch;

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
			new WidgetData
			{
				Name = "TitleWidget",
				Row = 4,
				Column = 13,
				RowSpan = 5,
				ColumnSpan = 14
			},

			new WidgetData
			{
				Name = "SearchWidget",
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

	private async Task AddWidgetsToGrid()
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
			
			// custom logic for advanced widgets:
			if (widget is SearchWidget searchWidget)
				searchWidget.OnSearch += s => OnSearch?.Invoke(s);
			
			// do not await each task at one time,
			// just add the task to a list and await all of them at one time
			initTasks.Add(widget.Initialize());
			
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
			"SearchWidget" => new SearchWidget(),
			"TitleWidget" => new TitleWidget(),
			"TimeDateWidget" => new TimeDateWidget(),
			_ => null
		};
	}

	public async Task Initialize()
	{
		await TryLoadWidgets();
		await AddWidgetsToGrid();
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