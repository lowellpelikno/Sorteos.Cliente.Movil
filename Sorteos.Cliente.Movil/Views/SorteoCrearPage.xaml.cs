using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
/// <summary>
    /// Pagina de formulario para la definicion, configuracion de reglas y creacion de un nuevo sorteo.
    /// </summary>
    public partial class SorteoCrearPage : ContentPage
    {
        /// <summary>
        /// Inicializa una nueva instancia de <see cref="SorteoCrearPage"/> enlazando su ViewModel.
        /// </summary>
        /// <param name="viewModel">Instancia inyectada de <see cref="SorteoCrearViewModel"/>.</param>
        public SorteoCrearPage(SorteoCrearViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

