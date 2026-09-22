using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
    public partial class PremioAsignacionDto : ObservableObject
    {
        public int IdPremio { get; set; }

        public int Lugar { get; set; }

        public string LugarTexto => $"{Lugar}º Lugar";

        public string DescripcionPremio { get; set; } = string.Empty;

        public string ValorFormateado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NumeroIngresado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int? NumeroGanador { get; set; }

        [ObservableProperty]
        public partial string NombreGanador { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string TelefonoGanador { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int? ReservaId { get; set; }

        [ObservableProperty]
        public partial bool EstaAsignado { get; set; }

        [ObservableProperty]
        public partial bool EsDesierto { get; set; }

        [ObservableProperty]
        public partial string ResumenResultado { get; set; } = string.Empty;
    }
}

