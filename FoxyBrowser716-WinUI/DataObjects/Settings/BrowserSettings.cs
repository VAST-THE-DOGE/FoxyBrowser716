namespace FoxyBrowser716_WinUI.DataObjects.Settings;

public partial class BrowserSettings : ObservableObject
{
    public List<ISetting> GetSettingControls()
    {
        return [];
    }
    
    //TODO: look more into this
    //TODO: webview2 and everything needs an ApplySettings function that will call at creation and non change of settings.
    /* Necessary Settings:
     *
     * Simple:
     * - performance for webview2
     * - performance for my UI
     * - other webview2 specific settings
     * - ai assistant settings
     * - tracking prevention
     * default zoom
     * color theme (dark/light mode)
     * startup mode: none, restore, config
     * restore browser on close?
     * download location
     * show download menu on download start
     * sleeping tabs and performance settings
     * reset to defaults
     * default search engine, last used or specific one
     * default browser
     * 
     * Complex:
     * Theme pick and editing
     * extension management
     * instance management
     * error viewer/reporter/log-exporter
     * startup config
     * cookies and permission management for websites
     * 
     */
    //TODO: need default static setting to use if none.
}