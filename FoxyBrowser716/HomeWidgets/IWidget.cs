using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using FoxyBrowser716.HomeWidgets.WidgetSettings;

namespace FoxyBrowser716.HomeWidgets
{
    public abstract class Widget : UserControl
    {
        public abstract string WidgetName { get; }
        public virtual bool CanAdd => true;
        public virtual bool CanRemove => true;
        public virtual int MinWidgetWidth => 1;
        public virtual int MinWidgetHeight => 1;
        public virtual int MaxWidgetWidth => 40;
        public virtual int MaxWidgetHeight => 20;
        
        public event Action? RemoveRequested;
        
        public virtual ICommand? RemoveCommand => CanRemove ? new RelayCommand(() => RemoveRequested?.Invoke()) : null;
        public virtual ICommand? SettingsCommand => WidgetSettings?.Count > 0 ? new RelayCommand(ShowSettings) : null;
        public virtual Dictionary<string, IWidgetSetting>? WidgetSettings { get; set; } = [];
        //TODO: move to a class that manages the names, default values, set values and serializing to/from JSON.
        
        public WidgetData Data { get; set; }
        
        protected TabManager TabManager;

        public virtual Task Initialize(TabManager manager, Dictionary<string, IWidgetSetting>? settings = null)
        {
            TabManager = manager;
            if (settings is { Count: > 0 } validSettings)
                WidgetSettings = validSettings;
            return Task.CompletedTask;
        }

        protected virtual void ShowSettings()
        {
            OpenWidgetSettings?.Invoke(WidgetSettings);
        }

        public event Action<Dictionary<string, IWidgetSetting>?>? OpenWidgetSettings;
        
        private bool _inWidgetEditingMode;
        public bool InWidgetEditingMode { get => _inWidgetEditingMode; set => SetEditMode(value); }
        private WidgetOverlayAdorner? _overlayAdorner;
        private SettingsAdorner? _settingsAdorner;

        private protected virtual void SetEditMode(bool value)
        {
            _inWidgetEditingMode = value;
            if (value)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null)
                {
                    _overlayAdorner = new WidgetOverlayAdorner(this);
                    adornerLayer.Add(_overlayAdorner);
                }
            }
            else
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (adornerLayer != null && _overlayAdorner != null)
                {
                    adornerLayer.Remove(_overlayAdorner);
                    _overlayAdorner = null;
                }
            }
        }
    }
    
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
    }
}