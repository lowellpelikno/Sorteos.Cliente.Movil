using System.Globalization;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// DTO que consolida las metricas ejecutivas, financieras y de inventario del negocio para la pantalla principal.
    /// </summary>
    public class DashboardResumenDto
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        // Metricas globales de conteo

        /// <summary>
        /// Total de sorteos con estatus Creado o En Juego.
        /// </summary>
        public int TotalSorteosActivos { get; set; }

        /// <summary>
        /// Cantidad de sorteos calendarizados para celebrarse en la fecha actual.
        /// </summary>
        public int SorteosHoy { get; set; }

        /// <summary>
        /// Cantidad total de clientes registrados y activos en el sistema.
        /// </summary>
        public int TotalClientes { get; set; }

        /// <summary>
        /// Cantidad total de premios disponibles en el catalogo general.
        /// </summary>
        public int TotalPremios { get; set; }

        // Metricas financieras

        /// <summary>
        /// Importe acumulado recaudado efectivamente por boletos pagados.
        /// </summary>
        public decimal TotalMontoPagado { get; set; }

        /// <summary>
        /// Importe total por cobrar correspondiente a boletos en estatus de apartado.
        /// </summary>
        public decimal TotalMontoApartado { get; set; }

        /// <summary>
        /// Importe financiero proyectado total (suma de montos pagados y apartados).
        /// </summary>
        public decimal TotalMontoProyectado => TotalMontoPagado + TotalMontoApartado;

        /// <summary>
        /// Monto pagado formateado como moneda mexicana ($#,##0.00).
        /// </summary>
        public string TotalMontoPagadoFormateado => TotalMontoPagado.ToString("C2", CulturaEsMx);

        /// <summary>
        /// Monto apartado formateado como moneda mexicana ($#,##0.00).
        /// </summary>
        public string TotalMontoApartadoFormateado => TotalMontoApartado.ToString("C2", CulturaEsMx);

        /// <summary>
        /// Monto proyectado formateado como moneda mexicana ($#,##0.00).
        /// </summary>
        public string TotalMontoProyectadoFormateado => TotalMontoProyectado.ToString("C2", CulturaEsMx);

        // Metricas de inventario de boletos

        /// <summary>
        /// Volumen total de boletos emitidos en todos los sorteos activos.
        /// </summary>
        public int TotalBoletosEmitidos { get; set; }

        /// <summary>
        /// Volumen de boletos con pago liquidado.
        /// </summary>
        public int TotalBoletosVendidos { get; set; }

        /// <summary>
        /// Volumen de boletos apartados pendientes de cobrar o confirmar.
        /// </summary>
        public int TotalApartadosPendientes { get; set; }

        /// <summary>
        /// Porcentaje numerico de ocupacion o colocacion sobre el inventario emitido (0.0 a 100.0).
        /// </summary>
        public double PorcentajeOcupacion =>
            TotalBoletosEmitidos > 0
                ? Math.Round(((double)TotalBoletosVendidos / TotalBoletosEmitidos) * 100.0, 1)
                : 0.0;

        /// <summary>
        /// Proporcion normalizada entre 0.0 y 1.0 para controles de barra de progreso visual (ProgressBar).
        /// </summary>
        public double ProporcionOcupacion =>
            TotalBoletosEmitidos > 0
                ? Math.Clamp((double)TotalBoletosVendidos / TotalBoletosEmitidos, 0.0, 1.0)
                : 0.0;

        /// <summary>
        /// Porcentaje de ocupacion formateado en texto (ej. "75.4%").
        /// </summary>
        public string PorcentajeOcupacionTexto => $"{PorcentajeOcupacion:F1}%";

        // Sorteo destacado

        /// <summary>
        /// Indica si existe al menos un sorteo activo apto para ser presentado como tarjeta destacada.
        /// </summary>
        public bool TieneSorteoDestacado { get; set; }

        /// <summary>
        /// Identificador primario del sorteo destacado.
        /// </summary>
        public int SorteoDestacadoId { get; set; }

        /// <summary>
        /// Folio comercial del sorteo destacado.
        /// </summary>
        public string SorteoDestacadoNumero { get; set; } = string.Empty;

        /// <summary>
        /// Titulo o descripcion general del sorteo destacado.
        /// </summary>
        public string SorteoDestacadoDescripcion { get; set; } = string.Empty;

        /// <summary>
        /// Precio por boleto o casilla del sorteo destacado.
        /// </summary>
        public decimal SorteoDestacadoCosto { get; set; }

        /// <summary>
        /// Precio por boleto formateado como moneda.
        /// </summary>
        public string SorteoDestacadoCostoFormateado => SorteoDestacadoCosto.ToString("C2", CulturaEsMx);

        /// <summary>
        /// Fecha programada de culminacion del sorteo destacado.
        /// </summary>
        public DateTime SorteoDestacadoFechaFin { get; set; } = DateTime.Today;

        /// <summary>
        /// Fecha fin formateada en dd/MM/yyyy.
        /// </summary>
        public string SorteoDestacadoFechaFinFormateada => SorteoDestacadoFechaFin.ToString("dd/MM/yyyy", CulturaEsMx);

        /// <summary>
        /// Sintesis en texto de los premios principales ofertados en el sorteo destacado.
        /// </summary>
        public string SorteoDestacadoPremiosResumen { get; set; } = string.Empty;

        /// <summary>
        /// Total de boletos que componen la cuadricula del sorteo destacado.
        /// </summary>
        public int SorteoDestacadoBoletosTotal { get; set; }

        /// <summary>
        /// Boletos pagados en el sorteo destacado.
        /// </summary>
        public int SorteoDestacadoBoletosPagados { get; set; }

        /// <summary>
        /// Boletos apartados pendientes de liquidar en el sorteo destacado.
        /// </summary>
        public int SorteoDestacadoBoletosApartados { get; set; }

        /// <summary>
        /// Nivel de avance o colocacion global (pagados + apartados) normalizado entre 0.0 y 1.0.
        /// </summary>
        public double SorteoDestacadoProgreso
        {
            get
            {
                if (SorteoDestacadoBoletosTotal <= 0) return 0.0;
                int ocupados = SorteoDestacadoBoletosPagados + SorteoDestacadoBoletosApartados;
                return Math.Clamp((double)ocupados / SorteoDestacadoBoletosTotal, 0.0, 1.0);
            }
        }

        /// <summary>
        /// Texto representativo del porcentaje de venta alcanzado en el sorteo destacado (ej. "65% vendido").
        /// </summary>
        public string SorteoDestacadoProgresoTexto
        {
            get
            {
                int porcentaje = (int)(SorteoDestacadoProgreso * 100);
                return $"{porcentaje}% vendido";
            }
        }
    }
}

