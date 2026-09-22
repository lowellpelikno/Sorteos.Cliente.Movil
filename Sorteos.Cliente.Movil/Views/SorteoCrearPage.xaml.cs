using Sorteos.Cliente.Movil.ViewModels;

namespace Sorteos.Cliente.Movil.Views
{
    public partial class SorteoCrearPage : ContentPage
    {
        public SorteoCrearPage(SorteoCrearViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}

