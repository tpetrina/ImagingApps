using PhotoBlender.ViewModels;

namespace PhotoBlender.Views
{
    public partial class MainPage
    {
        private readonly MainViewModel _vm;

        public MainPage()
        {
            InitializeComponent();

            DataContext = _vm = new MainViewModel();
            _vm.LoadAsync();
        }
    }
}