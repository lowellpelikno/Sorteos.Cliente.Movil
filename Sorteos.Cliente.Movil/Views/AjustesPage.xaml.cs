using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class AjustesPage : ContentPage
    {
        private readonly AjustesViewModel _viewModel;

        public AjustesPage(AjustesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await _viewModel.InicializarAsync();
            }
            catch (Exception)
            {
                // Salvaguarda defensiva para impedir fallos fatales en el ciclo de vida de UI
            }
        }
    }
}

