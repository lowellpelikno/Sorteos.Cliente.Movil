using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Modelo y DTO observable que representa un dia de la semana en la barra de navegacion semanal o en listas de programacion.
    /// Soporta proyecciones SQL directas de existencia de sorteos y logica temporal de dias pasados o vigentes.
    /// </summary>
    public partial class DiaSemanaItem : ObservableObject
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Identificador unico de la entidad base.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Numero secuencial del dia (1 = Lunes, ..., 7 = Domingo).
        /// </summary>
        public int IdDia { get; set; } // 1 = Lunes, ..., 7 = Domingo

        /// <summary>
        /// Nombre del dia en texto plano (ej. "Lunes", "Martes").
        /// </summary>
        public string NombreDia { get; set; } = string.Empty;

        /// <summary>
        /// Inicial o abreviatura del dia (L, M, M, J, V, S, D).
        /// </summary>
        public string InicialDia { get; set; } = string.Empty;

        /// <summary>
        /// Fecha calendario asignada al dia en la semana vigente.
        /// </summary>
        public DateTime Fecha { get; set; }

        // Mapeado desde proyeccion SQL en ObtenerDiasSemanaAsync (EXISTS(...) AS TieneSorteo)
        /// <summary>
        /// Indica si el dia cuenta con al menos un sorteo programado o registrado (proyectado directamente desde SQLite).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PuedeCrearSorteo))]
        public partial bool TieneSorteo { get; set; }

        /// <summary>
        /// Estado visual de seleccion interactiva del dia en la barra superior.
        /// </summary>
        [ObservableProperty]
        public partial bool EsSeleccionado { get; set; }

        /// <summary>
        /// Indica si el elemento actua como tarjeta informativa de fin de semana o politicas de negocio.
        /// </summary>
        [ObservableProperty]
        public partial bool EsTarjetaInformativa { get; set; }

        /// <summary>
        /// Determina si la fecha del elemento corresponde exactamente a la fecha de hoy.
        /// </summary>
        public bool EsHoy => !EsTarjetaInformativa && Fecha.Date == DateTime.Today;

        /// <summary>
        /// Determina si la fecha del dia ya concluyo con respecto a la fecha actual.
        /// </summary>
        public bool EsDiaPasado => !EsTarjetaInformativa && Fecha.Date < DateTime.Today;

        /// <summary>
        /// Evalua si es posible dar de alta un sorteo nuevo en este dia (debe ser un dia futuro u hoy, sin sorteo previo).
        /// </summary>
        public bool PuedeCrearSorteo => !EsTarjetaInformativa && !TieneSorteo && !EsDiaPasado;

        /// <summary>
        /// Digito del dia formateado a dos digitos (ej. "05", "14").
        /// </summary>
        public string NumeroDia => EsTarjetaInformativa ? "Info" : Fecha.ToString("dd", CulturaEsMx);

        /// <summary>
        /// Nombre en texto del mes al que pertenece la fecha (ej. "octubre").
        /// </summary>
        public string NombreMes => EsTarjetaInformativa ? string.Empty : Fecha.ToString("MMMM", CulturaEsMx);

        /// <summary>
        /// Fecha completa legible en espanol (ej. "martes, 23 de septiembre").
        /// </summary>
        public string FechaFormateada => EsTarjetaInformativa ? "Reglas de Semana y Sorteos Pospuestos" : Fecha.ToString("dddd, dd 'de' MMMM", CulturaEsMx);
    }
}
