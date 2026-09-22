using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class TerminosCondicionesPage : ContentPage
    {
        public TerminosCondicionesPage(TerminosCondicionesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

