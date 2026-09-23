using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// DTO y modelo observable para representar a un cliente con sus boletos apartados o pagados en una pantalla o lista.
    /// Soporta seleccion multiple interactiva y deteccion de estado ganador.
    /// </summary>
    public partial class ClienteConApartadosDto : ObservableObject
    {
        /// <summary>
        /// Identificador de la reserva vinculada al apartado del cliente.
        /// </summary>
        [ObservableProperty]
        public partial int ReservaId { get; set; }

        /// <summary>
        /// Identificador del estatus de la reserva (1 = Apartado, 2 = Pagado, 3 = Liberado).
        /// </summary>
        [ObservableProperty]
        public partial int IdEstatus { get; set; }

        /// <summary>
        /// Nombre comercial o personal del cliente asociado a la reserva.
        /// </summary>
        [ObservableProperty]
        public partial string NombreCliente { get; set; } = string.Empty;

        /// <summary>
        /// Cadena formateada con la lista de numeros apartados por el cliente (ej. "01, 14, 25").
        /// </summary>
        [ObservableProperty]
        public partial string NumerosTexto { get; set; } = string.Empty;

        /// <summary>
        /// Cantidad total de numeros incluidos en esta reserva.
        /// </summary>
        [ObservableProperty]
        public partial int TotalNumeros { get; set; }

        /// <summary>
        /// Primer numero o numero representativo de la reserva para ordenamiento visual.
        /// </summary>
        [ObservableProperty]
        public partial int PrimerNumero { get; set; }

        /// <summary>
        /// Metodo o forma de liquidacion especificada para la reserva (ej. Efectivo, Transferencia).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EsPagoElectronico))]
        public partial string FormaDePago { get; set; } = "Efectivo";

        /// <summary>
        /// Ruta local o enlace al archivo de comprobante digital adjunto.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TieneComprobante))]
        [NotifyPropertyChangedFor(nameof(NoTieneComprobante))]
        public partial string ComprobanteUrl { get; set; } = string.Empty;

        /// <summary>
        /// Estado de seleccion interactiva del elemento en listas de accion por lote.
        /// </summary>
        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        /// <summary>
        /// Callback delegado que se dispara ante cambios en la seleccion interactiva para recalcular totales en la vista.
        /// </summary>
        public Action? AlCambiarSeleccion { get; set; }

        partial void OnEstaSeleccionadoChanged(bool value)
        {
            AlCambiarSeleccion?.Invoke();
        }

        /// <summary>
        /// Indica si el pago fue registrado por medios electronicos distintos a efectivo.
        /// </summary>
        public bool EsPagoElectronico => !string.Equals(FormaDePago, "Efectivo", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Indica si la reserva cuenta con un comprobante adjunto registrado.
        /// </summary>
        public bool TieneComprobante   => !string.IsNullOrWhiteSpace(ComprobanteUrl);

        /// <summary>
        /// Indica si la reserva carece de comprobante digital adjunto.
        /// </summary>
        public bool NoTieneComprobante => !TieneComprobante;

        /// <summary>
        /// Banderin que denota si la reserva contiene algun boleto ganador del sorteo.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial bool EsGanador { get; set; }

        /// <summary>
        /// Digito o valor numerico del boleto favorecido con un premio.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial int NumeroGanador { get; set; }

        /// <summary>
        /// Descripcion del premio obtenido por el boleto premiado.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LeyendaGanador))]
        public partial string PremioGanadoTexto { get; set; } = string.Empty;

        /// <summary>
        /// Leyenda formateada que se despliega en la interfaz para anunciar al ganador.
        /// </summary>
        public string LeyendaGanador => EsGanador
            ? (!string.IsNullOrWhiteSpace(PremioGanadoTexto)
                ? $"¡GANADOR! Boleto #{NumeroGanador:D2} — {PremioGanadoTexto}"
                : $"¡GANADOR! Boleto #{NumeroGanador:D2}")
            : string.Empty;
    }
}
