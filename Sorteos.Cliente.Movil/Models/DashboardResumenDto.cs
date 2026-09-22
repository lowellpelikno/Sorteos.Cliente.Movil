using System.Globalization;

namespace Sorteos.Cliente.Movil.Models
{
    public class DashboardResumenDto
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        // Metricas globales de conteo
        public int TotalSorteosActivos { get; set; }
        public int SorteosHoy { get; set; }
        public int TotalClientes { get; set; }
        public int TotalPremios { get; set; }

        // Metricas financieras
        public decimal TotalMontoPagado { get; set; }
        public decimal TotalMontoApartado { get; set; }
        public decimal TotalMontoProyectado => TotalMontoPagado + TotalMontoApartado;

        public string TotalMontoPagadoFormateado => TotalMontoPagado.ToString("C2", CulturaEsMx);
        public string TotalMontoApartadoFormateado => TotalMontoApartado.ToString("C2", CulturaEsMx);
        public string TotalMontoProyectadoFormateado => TotalMontoProyectado.ToString("C2", CulturaEsMx);

        // Metricas de inventario de boletos
        public int TotalBoletosEmitidos { get; set; }
        public int TotalBoletosVendidos { get; set; }
        public int TotalApartadosPendientes { get; set; }

        public double PorcentajeOcupacion =>
            TotalBoletosEmitidos > 0
                ? Math.Round(((double)TotalBoletosVendidos / TotalBoletosEmitidos) * 100.0, 1)
                : 0.0;

        public double ProporcionOcupacion =>
            TotalBoletosEmitidos > 0
                ? Math.Clamp((double)TotalBoletosVendidos / TotalBoletosEmitidos, 0.0, 1.0)
                : 0.0;

        public string PorcentajeOcupacionTexto => $"{PorcentajeOcupacion:F1}%";

        // Sorteo destacado
        public bool TieneSorteoDestacado { get; set; }
        public int SorteoDestacadoId { get; set; }
        public string SorteoDestacadoNumero { get; set; } = string.Empty;
        public string SorteoDestacadoDescripcion { get; set; } = string.Empty;
        public decimal SorteoDestacadoCosto { get; set; }
        public string SorteoDestacadoCostoFormateado => SorteoDestacadoCosto.ToString("C2", CulturaEsMx);
        public DateTime SorteoDestacadoFechaFin { get; set; } = DateTime.Today;
        public string SorteoDestacadoFechaFinFormateada => SorteoDestacadoFechaFin.ToString("dd/MM/yyyy", CulturaEsMx);
        public string SorteoDestacadoPremiosResumen { get; set; } = string.Empty;
        public int SorteoDestacadoBoletosTotal { get; set; }
        public int SorteoDestacadoBoletosPagados { get; set; }
        public int SorteoDestacadoBoletosApartados { get; set; }

        public double SorteoDestacadoProgreso
        {
            get
            {
                if (SorteoDestacadoBoletosTotal <= 0) return 0.0;
                int ocupados = SorteoDestacadoBoletosPagados + SorteoDestacadoBoletosApartados;
                return Math.Clamp((double)ocupados / SorteoDestacadoBoletosTotal, 0.0, 1.0);
            }
        }

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

