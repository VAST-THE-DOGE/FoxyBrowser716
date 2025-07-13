using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Material.Icons;
using Material.Icons.WPF;
using static FoxyBrowser716.Styling.Animator;
using static FoxyBrowser716.Styling.ColorPalette;

namespace FoxyBrowser716;

public partial class InstanceManagerRow : UserControl
{
	
	public event Action<InstanceManager> OnDelete;
	public event Action<InstanceManager> OnDuplicate;
	public event Action<InstanceManager, string> OnRename;
	
	private InstanceManager _instance;
	
	public InstanceManagerRow(InstanceManager instance, bool currentInstance)
	{
		InitializeComponent();
		
		_instance = instance;

		NameLabel.Content = instance.InstanceName;

		//TEMP
		DuplicateButton.Visibility = Visibility.Collapsed;
		
		if (currentInstance || instance.PrimaryInstance)
		{
			//TEMP
			DeleteButton.Visibility = Visibility.Hidden;
			EditButton.Visibility = Visibility.Hidden;
			
			FavoriteButton.Content = new MaterialIcon { Kind = MaterialIconKind.Star };
		}
		
		var hoverColor = Color.FromArgb(50,255,255,255);
		
		foreach (var b in (Button[])
		         [
			         EditButton, ConfirmButton, CancelButton, DeleteButton, DuplicateButton
		         ])
		{
			b.MouseEnter += (_, _) => { ChangeColorAnimation(b.Background, Colors.Transparent, hoverColor); };
			b.MouseLeave += (_, _) => { ChangeColorAnimation(b.Background, hoverColor, Colors.Transparent); };
		}

		EditButton.Click += (_, _) =>
		{
			EditButton.Visibility = Visibility.Collapsed;
			NameLabel.Visibility = Visibility.Collapsed;
			
			InputBackground.Visibility = Visibility.Visible;
			NameInputBox.Text = "";
			CancelButton.Visibility = Visibility.Visible;
		};

		ConfirmButton.Click += (_, _) =>
		{
			var name = NameInputBox.Text;
			if (InstanceManagerControl.ValidateName(name).isValid)
			{
				OnRename?.Invoke(_instance, name);
				
				// will refresh grid, so do not worry about recovering here
			}
		};
		
		CancelButton.Click += (_, _) =>
		{
			EditButton.Visibility = Visibility.Visible;
			NameLabel.Visibility = Visibility.Visible;
			
			InputBackground.Visibility = Visibility.Collapsed;
			ConfirmButton.Visibility = Visibility.Collapsed;
			CancelButton.Visibility = Visibility.Collapsed;
		};

		DeleteButton.Click += (_, _) =>
		{
			OnDelete?.Invoke(_instance);
		};
	}
	
	private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		UpdatePlaceholderVisibility();
		if (CancelButton.Visibility == Visibility.Visible)
		{
			ConfirmButton.Visibility = InstanceManagerControl
				.ValidateName(NameInputBox.Text).isValid 
				? Visibility.Visible 
				: Visibility.Collapsed;
		}
	}
	
	private void TextBox_GotFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}
    
	private void TextBox_LostFocus(object sender, RoutedEventArgs e)
	{
		UpdatePlaceholderVisibility();
	}

	private void UpdatePlaceholderVisibility()
	{
		PlaceholderText.Visibility = string.IsNullOrEmpty(NameInputBox.Text) && !NameInputBox.IsFocused 
			? Visibility.Visible 
			: Visibility.Collapsed;
	}
}