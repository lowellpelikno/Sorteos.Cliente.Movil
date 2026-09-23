using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina que despliega el directorio interactivo de clientes con busqueda reactiva, resumen de numeros de planta y acciones de edicion.
    /// </summary>
    public partial class ClienteListPage : ContentPage
    {
        private readonly ClienteListViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClienteListPage"/> vinculando el ViewModel correspondiente.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="ClienteListViewModel"/>.</param>
        public ClienteListPage(ClienteListViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        /// <summary>
        /// Ciclo de vida visual que dispara la carga inicial de clientes de forma perezosa sin recarga indiscriminada en retornos.
        /// </summary>
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

