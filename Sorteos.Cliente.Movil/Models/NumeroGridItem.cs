using CommunityToolkit.Mvvm.ComponentModel;

namespace Sorteos.Cliente.Movil.Models
{
    public partial class NumeroGridItem : ObservableObject
    {
        [ObservableProperty]
        public partial int NumeroValor { get; set; }

        [ObservableProperty]
        public partial string NumeroFormateado { get; set; } = string.Empty;

        [ObservableProperty]
        public partial int? ReservaId { get; set; }

        [ObservableProperty]
        public partial int? ClienteId { get; set; }

        [ObservableProperty]
        public partial string NombreCliente { get; set; } = string.Empty;

        [ObservableProperty]
        public partial EstadoNumeroSorteo Estado { get; set; } = EstadoNumeroSorteo.Disponible;

        [ObservableProperty]
        public partial bool EsCombo { get; set; }

        [ObservableProperty]
        public partial string TextoCombo { get; set; } = string.Empty;

        [ObservableProperty]
        public partial List<int> NumerosCombo { get; set; } = [];

        [ObservableProperty]
        public partial string BadgeTexto { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool EstaSeleccionado { get; set; }

        public bool EstaDisponible => Estado == EstadoNumeroSorteo.Disponible;
        public bool EstaApartado => Estado == EstadoNumeroSorteo.Apartado;
        public bool EstaPagado => Estado == EstadoNumeroSorteo.Pagado;
        public bool EsGanador => Estado == EstadoNumeroSorteo.Ganador;

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

