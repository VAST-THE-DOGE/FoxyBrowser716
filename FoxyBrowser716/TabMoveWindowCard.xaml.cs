﻿using System;
using System.Windows;
using System.Windows.Input;

namespace FoxyBrowser716
{
    public partial class TabMoveWindowCard : Window
    {
        private WebsiteTab _tab;
        private ServerManager _instance;
        private bool _isDragging = true;
        private bool _dragInitiated = false;
        private double _startTime;

        public TabMoveWindowCard(ServerManager instance, WebsiteTab tab)
        {
            InitializeComponent();
            _instance = instance;
            _tab = tab;

            _startTime = DateTime.Now.Millisecond;
            
            TitleLabel.Content = _tab.Title;
            TabIcon.Child = _tab.Icon;

            Loaded += (s, e) =>
            {
                
            };

            // Hook up event handlers
            LocationChanged += Window_LocationChanged;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        private async void Window_LocationChanged(object sender, EventArgs e)
        {
            try
            {
                if (_instance.DoPositionUpdate(Left, Top) && DateTime.Now.Millisecond > _startTime + 200 && await _instance.TryTabTransfer(_tab, Left, Top))
                {
                    _isDragging = false;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                if (!_dragInitiated)
                {
                    _dragInitiated = true;
                    this.DragMove(); // Initiates system drag with snapping
                    // After DragMove returns, the drag is complete (mouse button released)
                    OnMouseLeftButtonUp(sender, null);
                }
            }
        }

        private async void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs? e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            var finalRect = new Rect(Left, Top, ActualWidth, ActualHeight);
            await _instance.CreateWindowFromTab(_tab, finalRect, WindowState == WindowState.Maximized);
            Close();
        }
    }
}