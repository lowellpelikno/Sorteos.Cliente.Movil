using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
    public partial class ClienteConApartadosDto : ObservableObject
    {
        [ObservableProperty]
        public partial int ReservaId { get; set; }

        [ObservableProperty]
        public partial int IdEstatus { get; set; }

        [ObservableProperty]
        public partial string NombreCliente { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string NumerosTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int TotalNumeros { get; set; }

        [ObservableProperty]
        public partial int PrimerNumero { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsPagoElectronico))]
        public partial string FormaDePago { get; set; } = "Efectivo";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TieneComprobante))]
        [NotifyPropertyChangedFor(nameof(NoTieneComprobante))]
        public partial string ComprobanteUrl { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        public Action? AlCambiarSeleccion { get; set; }

        partial void OnEstaSeleccionadoChanged(bool value)
        {
            AlCambiarSeleccion?.Invoke();
        }

        public bool EsPagoElectronico => !string.Equals(FormaDePago, "Efectivo", StringComparison.OrdinalIgnoreCase);
        public bool TieneComprobante   => !string.IsNullOrWhiteSpace(ComprobanteUrl);
        public bool NoTieneComprobante => !TieneComprobante;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial bool EsGanador { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial int NumeroGanador { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial string PremioGanadoTexto { get; set; } = string.Empty;

        public string LeyendaGanador => EsGanador
            ? (!string.IsNullOrWhiteSpace(PremioGanadoTexto)
                ? $"¡GANADOR! Boleto #{NumeroGanador:D2} — {PremioGanadoTexto}"
                : $"¡GANADOR! Boleto #{NumeroGanador:D2}")
            : string.Empty;
    }
}
