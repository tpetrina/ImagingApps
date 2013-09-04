using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AdvancedPhotoFilter.SelectionTools;
using AdvancedPhotoFilter.ViewModels;

namespace AdvancedPhotoFilter.Views
{
    public partial class ToolsControl
    {
        public static readonly DependencyProperty WrapperGridProperty =
            DependencyProperty.Register("WrapperGrid", typeof(Grid), typeof(ToolsControl), new PropertyMetadata(default(Grid), (
                o, args) =>
                {
                    ((ToolsControl)o)._wrapperGrid = args.NewValue as Grid;
                }));

        public Grid WrapperGrid
        {
            get { return (Grid)GetValue(WrapperGridProperty); }
            set { SetValue(WrapperGridProperty, value); }
        }

        private FrameworkElement _visualToolElement;
        private IVisualTool _currentVisualTool;
        private Grid _wrapperGrid;

        public ToolsControl()
        {
            InitializeComponent();
        }

        private void rectTools_MouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            UnfoldToolsMenu();
        }

        private void rectTools_Tap(object sender, GestureEventArgs e)
        {
            UnfoldToolsMenu();
        }

        private void UnfoldToolsMenu()
        {
            var vm = DataContext as ToolsViewModel;
            if (vm != null)
                vm.IsShown = !vm.IsShown;
        }

        private void toolsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (toolsList.SelectedItem == null)
                return;

            if (_currentVisualTool != null)
            {
                _currentVisualTool.ElementChanged -= CurrentVisualToolOnElementChanged;
                _currentVisualTool.PositionChanged -= CurrentVisualToolOnPositionChanged;
            }

            UnfoldToolsMenu();
            App.MainViewModel.SelectedTool = ((ToolItemViewModel)toolsList.SelectedItem).Tool;

            _currentVisualTool = App.MainViewModel.SelectedTool as IVisualTool;
            if (_currentVisualTool != null)
            {
                _currentVisualTool.ElementChanged += CurrentVisualToolOnElementChanged;
                _currentVisualTool.PositionChanged += CurrentVisualToolOnPositionChanged;
            }
        }

        private void CurrentVisualToolOnElementChanged(object sender, EventArgs eventArgs)
        {
            if (_visualToolElement != null)
            {
                _wrapperGrid.Children.Remove(_visualToolElement);
                _visualToolElement = null;
            }

            if (_currentVisualTool != null)
            {
                _visualToolElement = _currentVisualTool.Element;
                if (_visualToolElement != null)
                {
                    _wrapperGrid.Children.Add(_visualToolElement);
                }
            }
        }

        private void CurrentVisualToolOnPositionChanged(object sender, EventArgs eventArgs)
        {
            if (_visualToolElement != null && _currentVisualTool != null)
            {
                _visualToolElement.Margin = new Thickness(_currentVisualTool.Position.X, _currentVisualTool.Position.Y, 0, 0);
            }
        }
    }
}
