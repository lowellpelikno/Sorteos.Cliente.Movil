using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
    [Table("SorteoPlantilla")]
    public class SorteoPlantilla
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int IdDiasSorteo { get; set; } // 1 a 7

        [Indexed]
        public int IdEstatusSorteo { get; set; } = 1; // 1=Creado/Activo, 2=En Juego, 3=Finalizado

        public string Descripcion { get; set; } = string.Empty;

        public string NumeroDeSorteo { get; set; } = string.Empty;

        public string SeJuegaCon { get; set; } = string.Empty; // Ej. Loteria Nacional

        public decimal Costo { get; set; }

        public int Oportunidades { get; set; } = 1; // 1, 2, 3, 4

        public int CantidadNumeros { get; set; } = 100; // 100, 1000, etc.

        public DateTime FechaInicio { get; set; } = DateTime.Today;

        public DateTime FechaFin { get; set; } = DateTime.Today;

        public bool RepetirCadaSemana { get; set; } = false;

        public bool Activo { get; set; } = true;

        public string? RutaImagen { get; set; }

        [Ignore]
        public bool TieneImagen => !string.IsNullOrWhiteSpace(RutaImagen) && File.Exists(RutaImagen);

        [Ignore]
        public bool EsSorteoFinalizado => IdEstatusSorteo == 3;

        [Ignore]
        public string CostoFormateado => Costo.ToString("C2", CulturaEsMx);

        [Ignore]
        public string OportunidadesTexto => Oportunidades == 1 ? "1 Oportunidad" : $"{Oportunidades} Oportunidades";

        [Ignore]
        public string CantidadNumerosTexto => $"{CantidadNumeros} Números";

        [Ignore]
        public int CantidadEmisiones => Oportunidades > 1
            ? (CantidadNumeros / Oportunidades) + (CantidadNumeros % Oportunidades)
            : CantidadNumeros;

        [Ignore]
        public string CantidadEmisionesTexto => $"{CantidadEmisiones} Emisiones";

        [Ignore]
        public string ResumenReglas => $"{OportunidadesTexto} | {CantidadNumerosTexto} | {CostoFormateado}";

        [Ignore]
        public string PeriodoTexto => $"{FechaInicio.ToString("dd/MM/yyyy", CulturaEsMx)} - {FechaFin.ToString("dd/MM/yyyy", CulturaEsMx)}";
    }
}

