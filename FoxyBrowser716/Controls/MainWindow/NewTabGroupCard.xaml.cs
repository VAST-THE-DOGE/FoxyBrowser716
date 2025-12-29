using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FoxyBrowser716.Controls.Generic;
using FoxyBrowser716.DataObjects.Basic;
using FoxyBrowser716.DataObjects.Complex;
using Material.Icons;
using Material.Icons.WinUI3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FoxyBrowser716.Controls.MainWindow;

[ObservableObject]
public sealed partial class NewTabGroupCard : UserControl
{
    public bool IsCollapsed { get; private set; }

    private FRGBInput? rgbInput;
    
    public TabGroup TabGroup
    {
        get;
        set
        {
            SetProperty(ref field, value);

            if (rgbInput is not null)
            {
                contextStack.Children.Remove(rgbInput);
            }

            rgbInput = new FRGBInput(TabGroup.GroupColor.A, TabGroup.GroupColor.R, TabGroup.GroupColor.G, TabGroup.GroupColor.B)
            {
                CurrentTheme = CurrentTheme,
            };
            rgbInput.OnValueChanged += c =>
            {
                TabGroup.GroupColor = c;
            };
            contextStack.Children.Add(rgbInput);
        }
    }
    
    public NewTabGroupCard()
    {
        InitializeComponent();
    }

    public Theme CurrentTheme
    {
        get;
        set
        {
            SetProperty(ref field, value);
            rgbInput?.CurrentTheme = CurrentTheme;
            ButtonClose.CurrentTheme = CurrentTheme with {SecondaryForegroundColor = CurrentTheme.NoColor, PrimaryHighlightColor = CurrentTheme.NoColor};
        }
    } = DefaultThemes.DarkMode;
    
    private void ButtonCollapse_OnOnClick(object sender, RoutedEventArgs e)
    {
        SetCollapsed(!IsCollapsed);
    }

    public void SetCollapsed(bool isCollapsed)
    {
        IsCollapsed = isCollapsed;
        TabHolder.Visibility = IsCollapsed ? Visibility.Collapsed : Visibility.Visible;
        if (ButtonCollapse.Content is MaterialIcon icon)
            icon.Kind = IsCollapsed ? MaterialIconKind.ExpandMore : MaterialIconKind.ExpandLess;
    }
    
    private void TabCard_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // ButtonCollapse.Visibility = e.NewSize.Width > Icon.Width + ButtonCollapse.Width ? Visibility.Visible : Visibility.Collapsed;
    }

    private void TabCard_OnClick(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
            TabGroup.TabManager.SwapActiveTabTo(wt.Id);
    }

    private void TabCard_OnCloseRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
            TabGroup.TabManager.RemoveTab(wt.Id);
    }

    private async void TabCard_OnDuplicateRequested(NewTabCard obj)
    {
        if (obj.Tag is WebviewTab wt)
        {
            var id = TabGroup.TabManager.AddTab(wt.Info.Url);
            TabGroup.TabManager.MoveTabToGroup(id,TabGroup.Id);
            TabGroup.TabManager.SwapActiveTabTo(id);
        }
    }

// Group card drag events (for receiving tabs from outside)
    private void Root_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("WebviewTab"))
        {
            e.AcceptedOperation = DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Add to {TabGroup.Name}";
            
            // Visual feedback - highlight the group
            Root.Opacity = 0.7;
        }
        else
        {
            e.AcceptedOperation = DataPackageOperation.None;
        }
    }

    private void Root_Drop(object sender, DragEventArgs e)
    {
        Root.Opacity = 1.0;
        
        if (e.DataView.Properties.TryGetValue("WebviewTab", out var tabObj) && tabObj is WebviewTab tab)
        {
            TabGroup.TabManager.MoveTabToGroup(tab.Id, TabGroup.Id);
        }
    }

    private void Root_DragEnter(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("WebviewTab"))
        {
            Root.Opacity = 0.7;
        }
    }

    private void Root_DragLeave(object sender, DragEventArgs e)
    {
        Root.Opacity = 1.0;
    }

    // Tabs within group drag events (for reordering within the group)
    private void TabsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is WebviewTab tab)
        {
            e.Data.Properties.Add("WebviewTab", tab);
            e.Data.Properties.Add("SourceGroupId", TabGroup.Id);
            e.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }

    private void TabsList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        // Reordering within the group is handled automatically by ListView
    }

    private void TabsList_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        
        if (e.DataView.Properties.ContainsKey("SourceGroupId"))
        {
            e.DragUIOverride.Caption = "Reorder";
        }
        else
        {
            e.DragUIOverride.Caption = $"Move to {TabGroup.Name}";
        }
    }

    private void TabsList_Drop(object sender, DragEventArgs e)
    {
        var listView = sender as ListView;
        
        // If dropping from another group, add the tab
        if (e.DataView.Properties.TryGetValue("WebviewTab", out var tabObj) && 
            tabObj is WebviewTab tab &&
            e.DataView.Properties.TryGetValue("SourceGroupId", out var sourceGroupIdObj) &&
            sourceGroupIdObj is int sourceGroupId &&
            sourceGroupId != TabGroup.Id)
        {
            // Find the drop position
            var position = e.GetPosition(listView);
            int targetIndex = TabGroup.Tabs.Count; // Default to end
            
            if (listView?.Items != null)
            {
                for (int i = 0; i < listView.Items.Count; i++)
                {
                    var container = listView.ContainerFromIndex(i) as ListViewItem;
                    if (container != null)
                    {
                        var bounds = container.TransformToVisual(listView).TransformBounds(
                            new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                        
                        if (position.Y < bounds.Top + bounds.Height / 2)
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }
            }
            
            TabGroup.TabManager.MoveTabToGroup(tab.Id, TabGroup.Id, targetIndex);
        }
        // Otherwise, ListView handles reordering automatically
    }

    private void ButtonClose_OnOnClick(object sender, RoutedEventArgs e)
    {
        TabGroup.TabManager.RemoveGroup(TabGroup.Id);
    }

    private void DuplicateButton_OnOnClick(object sender, RoutedEventArgs e)
    {
        TabGroup? newGroup = null;
        
        foreach (var tab in TabGroup.Tabs)
        {
            var newTab = TabGroup.TabManager.AddTab(tab.Info.Url);

            newGroup ??= TabGroup.TabManager.CreateGroup();
            
            TabGroup.TabManager.MoveTabToGroup(newTab, newGroup.Id);
        }
        
        if (newGroup is null) return;
        
        TabGroup.TabManager.SwapActiveTabTo(newGroup.Tabs.First().Id);
        newGroup.Name = $"Copy of {newGroup.Name}";
        newGroup.GroupColor = TabGroup.GroupColor;
    }
}