using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class ClienteListPage : ContentPage
    {
        private readonly ClienteListViewModel _viewModel;

        public ClienteListPage(ClienteListViewModel viewModel)
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
                    await _viewModel.CargarClientesCommand.ExecuteAsync(null);
                }
            }
            catch (Exception)
            {
                // Salvaguarda defensiva para impedir fallos fatales en el ciclo de vida de UI
            }
        }
    }
}

