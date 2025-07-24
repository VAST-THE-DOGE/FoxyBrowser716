using Material.Icons;
using Microsoft.UI.Xaml.Markup;

namespace FoxyBrowser716_WinUI.Controls.Generic;

[MarkupExtensionReturnType(ReturnType = typeof(MaterialControlIcon))]
public sealed class MaterialControlIcon : PathIcon
{
	public static readonly DependencyProperty KindProperty =
		DependencyProperty.Register(
			nameof(Kind),
			typeof(MaterialIconKind),
			typeof(MaterialControlIcon),
			new PropertyMetadata(MaterialIconKind.AbTesting, OnKindChanged)
		);

	public MaterialIconKind Kind
	{
		get => (MaterialIconKind)GetValue(KindProperty);
		set => SetValue(KindProperty, value);
	}

	private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is MaterialControlIcon icon)
		{
			string pathData = MaterialIconDataProvider.GetData(icon.Kind);
			icon.Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), pathData);
		}
	}
}

[MarkupExtensionReturnType(ReturnType = typeof(MaterialMarkupIcon))]
public sealed class MaterialMarkupIcon : MarkupExtension
{
	public MaterialIconKind Kind { get; set; } = MaterialIconKind.AbTesting;
    
	protected override object ProvideValue()
	{
		return new MaterialControlIcon { Kind = Kind };
	}
}