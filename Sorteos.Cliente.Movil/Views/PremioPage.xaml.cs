using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class PremioPage : ContentPage
    {
        public PremioPage(PremioViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

