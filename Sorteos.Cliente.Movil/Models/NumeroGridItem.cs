using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Modelo y DTO observable que representa una casilla de numero en la cuadricula interactiva de ventas o consulta de un sorteo.
    /// Resuelve de forma reactiva los colores de fondo, bordes, tipografia y compatibilidad con paquetes combo o seleccion por lote.
    /// </summary>
    public partial class NumeroGridItem : ObservableObject
    {
        /// <summary>
        /// Valor numerico entero asignado al casillero.
        /// </summary>
        [ObservableProperty]
        public partial int NumeroValor { get; set; }

        /// <summary>
        /// Representacion en texto formateada con ceros a la izquierda (ej. "07", "042").
        /// </summary>
        [ObservableProperty]
        public partial string NumeroFormateado { get; set; } = string.Empty;

        /// <summary>
        /// Identificador de la reserva vinculada; null si permanece disponible.
        /// </summary>
        [ObservableProperty]
        public partial int? ReservaId { get; set; }

        /// <summary>
        /// Identificador del cliente que mantiene apartado o pagado el numero.
        /// </summary>
        [ObservableProperty]
        public partial int? ClienteId { get; set; }

        /// <summary>
        /// Nombre del comprador o titular asignado.
        /// </summary>
        [ObservableProperty]
        public partial string NombreCliente { get; set; } = string.Empty;

        /// <summary>
        /// Estado operativo y comercial de la casilla (Disponible, Apartado, Pagado, Ganador, Planta).
        /// </summary>
        [ObservableProperty]
        public partial EstadoNumeroSorteo Estado { get; set; } = EstadoNumeroSorteo.Disponible;

        /// <summary>
        /// Indica si la casilla forma parte de un paquete promocional o combo de boletos.
        /// </summary>
        [ObservableProperty]
        public partial bool EsCombo { get; set; }

        /// <summary>
        /// Texto representativo o etiqueta descriptiva del combo asociado.
        /// </summary>
        [ObservableProperty]
        public partial string TextoCombo { get; set; } = string.Empty;

        /// <summary>
        /// Coleccion de numeros vinculados en el paquete combo.
        /// </summary>
        [ObservableProperty]
        public partial List<int> NumerosCombo { get; set; } = [];

        /// <summary>
        /// Texto distintivo presentado en una insignia o badge sobre la casilla.
        /// </summary>
        [ObservableProperty]
        public partial string BadgeTexto { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el elemento se encuentra seleccionado por el usuario en la interfaz para reserva masiva.
        /// </summary>
        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        /// <summary>
        /// Indica si el numero esta libre y disponible para seleccion.
        /// </summary>
        public bool EstaDisponible => Estado == EstadoNumeroSorteo.Disponible;

        /// <summary>
        /// Indica si el numero se encuentra apartado pendiente de pago.
        /// </summary>
        public bool EstaApartado => Estado == EstadoNumeroSorteo.Apartado;

        /// <summary>
        /// Indica si el boleto se encuentra debidamente liquidado.
        /// </summary>
        public bool EstaPagado => Estado == EstadoNumeroSorteo.Pagado;

        /// <summary>
        /// Indica si el boleto resulto agraciado con algun premio del sorteo.
        /// </summary>
        public bool EsGanador => Estado == EstadoNumeroSorteo.Ganador;

        /// <summary>
        /// Color de fondo reactivo calculado segun el estado y seleccion del elemento.
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                if (EstaSeleccionado) return Color.FromArgb("#3F51B5"); // Indigo seleccion

                return Estado switch
                {
                    EstadoNumeroSorteo.Disponible => Color.FromArgb("#E8F5E9"), // Verde muy claro
                    EstadoNumeroSorteo.Apartado => Color.FromArgb("#FFF9C4"),   // Amarillo claro
                    EstadoNumeroSorteo.Pagado => Color.FromArgb("#FFCDD2"),     // Rojo suave
                    EstadoNumeroSorteo.Ganador => Color.FromArgb("#FFE082"),    // Dorado
                    EstadoNumeroSorteo.Planta => Color.FromArgb("#E8EAF6"),     // Indigo muy claro (azul violeta)
                    _ => Color.FromArgb("#F5F5F5")
                };
            }
        }

        /// <summary>
        /// Color perimetral de borde reactivo calculado segun el estado y seleccion del elemento.
        /// </summary>
        public Color BorderColor
        {
            get
            {
                if (EstaSeleccionado) return Color.FromArgb("#1A237E"); // Indigo oscuro

                return Estado switch
                {
                    EstadoNumeroSorteo.Disponible => Color.FromArgb("#4CAF50"), // Verde
                    EstadoNumeroSorteo.Apartado => Color.FromArgb("#FBC02D"),   // Amarillo
                    EstadoNumeroSorteo.Pagado => Color.FromArgb("#E53935"),     // Rojo
                    EstadoNumeroSorteo.Ganador => Color.FromArgb("#FFA000"),    // Dorado intenso
                    EstadoNumeroSorteo.Planta => Color.FromArgb("#7986CB"),     // Indigo medio
                    _ => Color.FromArgb("#BDBDBD")
                };
            }
        }

        /// <summary>
        /// Color tipografico de texto reactivo para garantizar alto contraste con el fondo.
        /// </summary>
        public Color TextColor
        {
            get
            {
                if (EstaSeleccionado) return Colors.White;

                return Estado switch
                {
                    EstadoNumeroSorteo.Disponible => Color.FromArgb("#1B5E20"),
                    EstadoNumeroSorteo.Apartado => Color.FromArgb("#F57F17"),
                    EstadoNumeroSorteo.Pagado => Color.FromArgb("#B71C1C"),
                    EstadoNumeroSorteo.Ganador => Color.FromArgb("#E65100"),
                    EstadoNumeroSorteo.Planta => Color.FromArgb("#1A237E"),
                    _ => Colors.Black
                };
            }
        }

        partial void OnEstaSeleccionadoChanged(bool value)
        {
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(TextColor));
        }

        partial void OnEstadoChanged(EstadoNumeroSorteo value)
        {
            OnPropertyChanged(nameof(EstaDisponible));
            OnPropertyChanged(nameof(EstaApartado));
            OnPropertyChanged(nameof(EstaPagado));
            OnPropertyChanged(nameof(EsGanador));
            OnPropertyChanged(nameof(BackgroundColor));
            OnPropertyChanged(nameof(BorderColor));
            OnPropertyChanged(nameof(TextColor));
        }
    }
}

