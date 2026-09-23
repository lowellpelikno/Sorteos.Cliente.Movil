using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de autenticacion local mediante PIN de seguridad para proteger el acceso operativo a la aplicacion.
    /// </summary>
    public partial class LoginPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="LoginPage"/> enlazando su ViewModel de autenticacion.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="LoginViewModel"/>.</param>
        public LoginPage(LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

