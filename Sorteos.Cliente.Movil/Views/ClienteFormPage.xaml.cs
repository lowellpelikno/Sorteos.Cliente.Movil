using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de formulario para el alta y edicion de clientes, administracion de numeros de planta y datos de contacto.
    /// </summary>
    public partial class ClienteFormPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClienteFormPage"/> enlazando su ViewModel.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="ClienteFormViewModel"/>.</param>
        public ClienteFormPage(ClienteFormViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

