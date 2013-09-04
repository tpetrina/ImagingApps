using System.Windows.Input;
using AdvancedPhotoFilter.ViewModels;

namespace AdvancedPhotoFilter.Views
{
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();

            ContentPanel.DataContext = App.MainViewModel;
            Tools.DataContext = new ToolsViewModel(App.MainViewModel.Tools);
            Tools.WrapperGrid = _imageWrapper;
        }

        #region Manipulation over image

        private void Img_OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            App.MainViewModel.ManipulationStarted(_mainImage.ActualWidth, _mainImage.ActualHeight, e);
            e.Handled = true;
        }

        private void Img_OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            App.MainViewModel.ManipulationDelta(e);
            e.Handled = true;
        }

        private void Img_OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            App.MainViewModel.ManipulationEnded(e);
            e.Handled = true;
        }

        #endregion
    }
}