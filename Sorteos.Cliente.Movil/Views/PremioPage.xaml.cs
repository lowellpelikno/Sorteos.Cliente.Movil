using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de formulario para el registro o edicion de un premio particular del catalogo general.
    /// </summary>
    public partial class PremioPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PremioPage"/> vinculando su ViewModel.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="PremioViewModel"/>.</param>
        public PremioPage(PremioViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

