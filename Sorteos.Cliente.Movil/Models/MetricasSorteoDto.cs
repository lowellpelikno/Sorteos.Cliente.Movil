namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// DTO que consolida los conteos de inventario y estados de los boletos correspondientes a un sorteo especifico.
    /// </summary>
    public class MetricasSorteoDto
    {
        /// <summary>
        /// Total absoluto de numeros que componen la cuadricula del sorteo.
        /// </summary>
        public int TotalNumeros { get; set; }

        /// <summary>
        /// Cantidad de numeros libres y disponibles para su asignacion o venta.
        /// </summary>
        public int TotalDisponibles { get; set; }

        /// <summary>
        /// Cantidad de numeros en estatus de apartado o reserva sin liquidar.
        /// </summary>
        public int TotalApartados { get; set; }

        /// <summary>
        /// Cantidad de numeros debidamente pagados.
        /// </summary>
        public int TotalPagados { get; set; }

        /// <summary>
        /// Cantidad de numeros que resultaron favorecidos con un premio en la celebracion del sorteo.
        /// </summary>
        public int TotalGanadores { get; set; }
    }
}

