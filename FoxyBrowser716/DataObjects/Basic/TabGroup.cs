using FoxyBrowser716.DataObjects.Complex;

namespace FoxyBrowser716.DataObjects.Basic;

public partial class TabGroup : ObservableObject
{
    private static int NextGroupId { get; set; } = 0;
    [ObservableProperty] public partial int GroupId { get; set; } = NextGroupId++;
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial Color GroupColor { get; set; } 
    
    [ObservableProperty] public partial ObservableCollection<WebviewTab> Tabs { get; set; } = [];
}