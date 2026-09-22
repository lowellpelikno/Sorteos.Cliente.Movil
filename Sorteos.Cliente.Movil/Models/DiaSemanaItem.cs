using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace Sorteos.Cliente.Movil.Models
{
    public partial class DiaSemanaItem : ObservableObject
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        public int Id { get; set; }

        public int IdDia { get; set; } // 1 = Lunes, ..., 7 = Domingo

        public string NombreDia { get; set; } = string.Empty;

        public string InicialDia { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        // Mapeado desde proyeccion SQL en ObtenerDiasSemanaAsync (EXISTS(...) AS TieneSorteo)
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PuedeCrearSorteo))]
        public partial bool TieneSorteo { get; set; }

        [ObservableProperty]
        public partial bool EsSeleccionado { get; set; }

        [ObservableProperty]
        public partial bool EsTarjetaInformativa { get; set; }

        public bool EsHoy => !EsTarjetaInformativa && Fecha.Date == DateTime.Today;

        public bool EsDiaPasado => !EsTarjetaInformativa && Fecha.Date < DateTime.Today;

        public bool PuedeCrearSorteo => !EsTarjetaInformativa && !TieneSorteo && !EsDiaPasado;

        public string NumeroDia => EsTarjetaInformativa ? "Info" : Fecha.ToString("dd", CulturaEsMx);

        public string NombreMes => EsTarjetaInformativa ? string.Empty : Fecha.ToString("MMMM", CulturaEsMx);

        public string FechaFormateada => EsTarjetaInformativa ? "Reglas de Semana y Sorteos Pospuestos" : Fecha.ToString("dddd, dd 'de' MMMM", CulturaEsMx);
    }
}
