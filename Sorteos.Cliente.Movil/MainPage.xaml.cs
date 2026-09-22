using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private bool _yaInicializado;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_yaInicializado)
            {
                _yaInicializado = true;
                await _viewModel.CargarDashboardAsync();
            }
        }
    }
}
