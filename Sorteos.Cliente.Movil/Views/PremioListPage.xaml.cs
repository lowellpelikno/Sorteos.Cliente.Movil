using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina que despliega el catalogo general de premios configurados en el sistema con opciones de alta, edicion y baja.
    /// </summary>
    public partial class PremioListPage : ContentPage
    {
        private readonly PremioListViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioListPage"/> vinculando el ViewModel correspondiente.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="PremioListViewModel"/>.</param>
        public PremioListPage(PremioListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        /// <summary>
        /// Ciclo de vida visual que dispara la carga inicial de premios de forma controlada sin recarga indiscriminada al retornar de otras vistas.
        /// </summary>
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

