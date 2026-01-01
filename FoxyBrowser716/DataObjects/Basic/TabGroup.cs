using FoxyBrowser716.DataManagement;
using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;

public partial class TabGroup : ObservableObject
{
    private static int NextGroupId { get; set; } = 0;
    [ObservableProperty] public partial int Id { get; set; } = NextGroupId++;
    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial Color GroupColor { get; set; }
    
    [ObservableProperty] public partial ObservableCollection<WebviewTab> Tabs { get; set; } = [];
    [ObservableProperty] public partial TabManager TabManager { get; set; }

    public TabGroup(TabManager tabManager)
    {
        TabManager = tabManager;
        GroupColor = TabManager.Instance.CurrentTheme.SecondaryHighlightColor; 
    }

    public void UpdateTabManager(TabManager newManager)
    {
        TabManager = newManager;
    }
}