using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static FoxyBrowser716.Styling.ColorPalette;
using static FoxyBrowser716.Styling.Animator;

namespace FoxyBrowser716;

public partial class InstanceManagerControl : UserControl
{
	private InstanceManager _ownerInstance;
	
	public InstanceManagerControl(InstanceManager ownerInstance)
	{
		InitializeComponent();

		_ownerInstance = ownerInstance;

		RefreshGrid();
		
		CreateButton.MouseEnter += (_, _) =>
		{
			ChangeColorAnimation(CreateButton.Background, AccentColor, ValidateName(NameInputBox.Text).isValid ? YesColorTransparent : NoColorTransparent);
		};
		CreateButton.MouseLeave += (_, _) =>
		{
			ChangeColorAnimation(CreateButton.Background, ValidateName(NameInputBox.Text).isValid ? YesColorTransparent : NoColorTransparent, AccentColor);
		};

		CreateButton.MouseDown += (_, e) =>
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var name = NameInputBox.Text;
				
				if (!ValidateName(name).isValid) return;

				NameInputBox.Text = "";
				
				var popup = new FoxyPopup()
				{
					Title = "Creating Instance",
					Subtitle = "Creating files...",
					ShowProgressbar = true,
				};
				popup.Show();

				var newInstancePath = Path.Combine(InfoGetter.InstanceFolder, name);
				Directory.CreateDirectory(newInstancePath);
				
				popup.Subtitle = "Adding instance...";
				var newInstance = new InstanceManager(name);
				ServerManager.Context.AllInstanceManagers.Add(newInstance);
				
				popup.Title = "Instance created";
				popup.Subtitle = "You can open this instance via the drop down on the top left.";
				popup.ShowProgressbar = false;
				popup.SetButtons([
					new FoxyPopup.BottomButton(() => popup.Close(), "Okay")
				]);
				
				RefreshGrid();
			}
		};
	}
	
	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}
	
	private void TextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
		ErrorLabel.Visibility = Visibility.Visible;
	}
    
	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
		ErrorLabel.Visibility = Visibility.Collapsed;
	}
	
	private void UpdatePlaceholderVisibility()
	{
		PlaceholderText.Visibility = string.IsNullOrEmpty(NameInputBox.Text) && !NameInputBox.IsFocused 
			? Visibility.Visible 
			: Visibility.Collapsed;
		ErrorLabel.Content = ValidateName(NameInputBox.Text).error;
	}

	public static (bool isValid, string? error) ValidateName(string name)
	{
		var sb = new StringBuilder();
		
		if (string.IsNullOrWhiteSpace(name))
		{
			sb.AppendLine("- Name cannot be empty.");
		}
		
		if (name.Length > 30)
		{
			sb.AppendLine("- Instance name cannot be longer than 30 characters.");
		}

		if (!name.All(c => char.IsLetterOrDigit(c) || c == ' '))
		{
			sb.AppendLine("- Instance name can only contain letters, numbers, and spaces.");
		}

		if (ServerManager.Context.AllInstanceManagers.Any(i => i.InstanceName == name))
		{
			sb.AppendLine("- Instance name already exists.");
		}
		return sb.Length == 0
			? (true, null)
			: (false, sb.ToString().TrimEnd());
	}

	private void RefreshGrid()
	{
		RowsHolder.Children.Clear();
		foreach (var instance in ServerManager.Context.AllInstanceManagers.OrderBy(i => i.InstanceName))
		{
			var row = new InstanceManagerRow(instance, instance.InstanceName == _ownerInstance.InstanceName);

			row.OnRename += async (manager, name) =>
			{
				await manager.RenameInstance(name);
				RefreshGrid();
			};

			row.OnDelete += async manager =>
			{
				if (manager.PrimaryInstance || manager.InstanceName == _ownerInstance.InstanceName) return;

				var popup = new FoxyPopup()
				{
					Title = "Are you sure?",
					Subtitle =
						$"Clicking 'Delete' will delete all browsing data, extensions, settings and any other info relating to the following instance:\n{manager.InstanceName}",
					ShowProgressbar = false,
				};
				popup.SetButtons([
					new FoxyPopup.BottomButton(() => popup.Close(), "Cancel"),
					new FoxyPopup.BottomButton(async () =>
					{
						popup.SetButtons();
						popup.Title = "Deleting Instance";
						popup.Subtitle = "Closing instance windows...";
						popup.ShowProgressbar = true;

						foreach (var window in manager.BrowserWindows)
						{
							window.Close();
						}
						
						popup.Subtitle = "Removing instance...";
						
						ServerManager.Context.AllInstanceManagers.Remove(manager);
						
						popup.Subtitle = "Deleting instance files...";

						if (!await SafeDeleteInstance(manager))
						{
							popup.Title = "Error Deleting Instance";
							popup.Subtitle = "Could not delete instance files. Please try again.";
							popup.ShowProgressbar = false;
							popup.SetButtons([
								new FoxyPopup.BottomButton(() => popup.Close(), "Okay")
							]);
							
							ServerManager.Context.AllInstanceManagers.Add(manager);
							
							return;
						}
						
						RefreshGrid();
						
						popup.Title = "Instance Deleted";
						popup.Subtitle = "";
						popup.ShowProgressbar = false;
						popup.SetButtons([
							new FoxyPopup.BottomButton(() => popup.Close(), "Okay")
						]);
					}, "Delete", Colors.Red)
				]);
				
				popup.Show();
			};
			
			RowsHolder.Children.Add(row);
		}
	}
	

	private bool HasDangerousAttributes(string path)
	{
		try
		{
			var attributes = File.GetAttributes(path);
			return attributes.HasFlag(FileAttributes.System) || 
			       attributes.HasFlag(FileAttributes.ReadOnly);
		}
		catch
		{
			return true;
		}
	}

	#region SafetyCheckForDeletingAFolder
	private bool IsPathSafeToDelete(string path)
	{
		try
		{
			string fullPath = Path.GetFullPath(path);
        
			if (!Directory.Exists(fullPath))
				return false;
        
			string expectedBasePath = Path.GetFullPath(InfoGetter.InstanceFolder);
        
			if (!fullPath.StartsWith(expectedBasePath, StringComparison.OrdinalIgnoreCase))
				return false;
        
			if (fullPath.Length < expectedBasePath.Length + 2)
				return false;
        
			if (!fullPath.Contains(Path.DirectorySeparatorChar + "Instances" + Path.DirectorySeparatorChar))
				return false;
        
			return true;
		}
		catch
		{
			return false;
		}
	}
	
	private async Task<bool> SafeDeleteInstance(InstanceManager manager)
	{
		if (!IsPathSafeToDelete(manager.InstanceFolder))
			return false;
		
		if (HasDangerousAttributes(manager.InstanceFolder))
			return false;
    
		try
		{
			if (string.IsNullOrWhiteSpace(manager.InstanceFolder))
				return false;
        
			Directory.Delete(manager.InstanceFolder, true);
			return true;
		}
		catch (Exception ex)
		{
			return false;
		}
	}
	#endregion
}