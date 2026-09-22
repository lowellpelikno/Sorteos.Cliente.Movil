using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class PremioListPage : ContentPage
    {
        private readonly PremioListViewModel _viewModel;

        public PremioListPage(PremioListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                if (!_viewModel.YaInicializado)
                {
                    await _viewModel.CargarPremiosCommand.ExecuteAsync(null);
                }
            }
            catch (Exception)
            {
                // Salvaguarda defensiva para impedir fallos fatales en el ciclo de vida de UI
            }
        }
    }
}

