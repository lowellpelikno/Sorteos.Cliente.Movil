using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil
{
/// <summary>
    /// Pagina de inicio y panel de control principal de la aplicacion que despliega las metricas ejecutivas, accesos rapidos y sorteo destacado.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;
        private bool _yaInicializado;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="MainPage"/> vinculando su ViewModel.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="MainViewModel"/>.</param>
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        /// <summary>
        /// Dispara la carga asincrona del panel ejecutivo de manera controlada al presentarse por primera vez la vista.
        /// </summary>
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
