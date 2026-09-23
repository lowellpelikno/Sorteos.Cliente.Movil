using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de aceptacion obligatoria de terminos y condiciones de uso previo al primer ingreso a la aplicacion.
    /// </summary>
    public partial class TerminosCondicionesPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TerminosCondicionesPage"/> enlazando su ViewModel.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="TerminosCondicionesViewModel"/>.</param>
        public TerminosCondicionesPage(TerminosCondicionesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

