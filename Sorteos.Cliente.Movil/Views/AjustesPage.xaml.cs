using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de configuracion y administracion global del negocio, preferencias de seguridad (PIN) y politicas de pago.
    /// </summary>
    public partial class AjustesPage : ContentPage
    {
        private readonly AjustesViewModel _viewModel;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="AjustesPage"/> vinculando su ViewModel correspondiente.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="AjustesViewModel"/>.</param>
        public AjustesPage(AjustesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>
        /// Metodo del ciclo de vida visual que inicializa de manera defensiva los ajustes del negocio al presentarse la vista.
        /// </summary>
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

