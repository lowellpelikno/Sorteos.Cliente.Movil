using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
    public partial class PremioSeleccionableItem : ObservableObject
    {
        public PremioLocal Premio { get; set; } = new();

        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        public PremioSeleccionableItem()
        {
        }

        public PremioSeleccionableItem(PremioLocal premio, bool seleccionado = false)
        {
            Premio = premio;
            EstaSeleccionado = seleccionado;
        }
    }
}

