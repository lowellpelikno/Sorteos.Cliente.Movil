using System.Globalization;
using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que representa un sorteo o rifa y su configuracion operativa y financiera.
    /// </summary>
    [Table("SorteoPlantilla")]
    public class SorteoPlantilla
    {
        private static readonly CultureInfo CulturaEsMx = new("es-MX");

        /// <summary>
        /// Identificador primario y autoincrementable del sorteo.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>
        /// Identificador del dia de la semana en el cual se celebra el sorteo (1 = Lunes, ..., 7 = Domingo).
        /// </summary>
        [Indexed]
        public int IdDiasSorteo { get; set; } // 1 a 7

        /// <summary>
        /// Identificador del estatus actual del sorteo (1 = Creado/Activo, 2 = En Juego, 3 = Finalizado).
        /// </summary>
        [Indexed]
        public int IdEstatusSorteo { get; set; } = 1; // 1=Creado/Activo, 2=En Juego, 3=Finalizado

        /// <summary>
        /// Titulo o descripcion principal del sorteo.
        /// </summary>
        public string Descripcion { get; set; } = string.Empty;

        /// <summary>
        /// Folio comercial unico asignado al sorteo (ej. "S-0001").
        /// </summary>
        public string NumeroDeSorteo { get; set; } = string.Empty;

        /// <summary>
        /// Referencia o base de juego externa utilizada para determinar los resultados (ej. "Loteria Nacional").
        /// </summary>
        public string SeJuegaCon { get; set; } = string.Empty; // Ej. Loteria Nacional

        /// <summary>
        /// Precio monetario individual de cada boleto o casilla.
        /// </summary>
        public decimal Costo { get; set; }

        /// <summary>
        /// Cantidad de oportunidades o combinaciones asociadas a cada boleto (1, 2, 3, 4).
        /// </summary>
        public int Oportunidades { get; set; } = 1; // 1, 2, 3, 4

        /// <summary>
        /// Total de casillas o numeros que conforman la cuadricula (100, 1000, etc.).
        /// </summary>
        public int CantidadNumeros { get; set; } = 100; // 100, 1000, etc.

        /// <summary>
        /// Fecha calendario de inicio o apertura de venta de boletos.
        /// </summary>
        public DateTime FechaInicio { get; set; } = DateTime.Today;

        /// <summary>
        /// Fecha limite o fecha de celebracion del sorteo.
        /// </summary>
        public DateTime FechaFin { get; set; } = DateTime.Today;

        /// <summary>
        /// Banderin que determina si el sorteo debe recrearse de forma recurrente semana con semana.
        /// </summary>
        public bool RepetirCadaSemana { get; set; } = false;

        /// <summary>
        /// Indica si el sorteo esta vigente y activo.
        /// </summary>
        public bool Activo { get; set; } = true;

        /// <summary>
        /// Ruta al archivo de imagen publicitaria o portada del sorteo en el almacenamiento local.
        /// </summary>
        public string? RutaImagen { get; set; }

        /// <summary>
        /// Comprueba si el sorteo cuenta con una imagen valida y existente en el sistema de archivos.
        /// </summary>
        [Ignore]
        public bool TieneImagen => !string.IsNullOrWhiteSpace(RutaImagen) && File.Exists(RutaImagen);

        /// <summary>
        /// Indica si el sorteo ya se encuentra en estatus Finalizado.
        /// </summary>
        [Ignore]
        public bool EsSorteoFinalizado => IdEstatusSorteo == 3;

        /// <summary>
        /// Precio por boleto formateado como moneda ($#,##0.00).
        /// </summary>
        [Ignore]
        public string CostoFormateado => Costo.ToString("C2", CulturaEsMx);

        /// <summary>
        /// Descripcion en texto de la cantidad de oportunidades (ej. "2 Oportunidades").
        /// </summary>
        [Ignore]
        public string OportunidadesTexto => Oportunidades == 1 ? "1 Oportunidad" : $"{Oportunidades} Oportunidades";

        /// <summary>
        /// Descripcion en texto del total de casillas (ej. "100 Números").
        /// </summary>
        [Ignore]
        public string CantidadNumerosTexto => $"{CantidadNumeros} Números";

        /// <summary>
        /// Cantidad calculada de emisiones o boletos fisicos resultantes segun las oportunidades configuradas.
        /// </summary>
        [Ignore]
        public int CantidadEmisiones => Oportunidades > 1
            ? (CantidadNumeros / Oportunidades) + (CantidadNumeros % Oportunidades)
            : CantidadNumeros;

        /// <summary>
        /// Texto representativo del conteo de emisiones.
        /// </summary>
        [Ignore]
        public string CantidadEmisionesTexto => $"{CantidadEmisiones} Emisiones";

        /// <summary>
        /// Resumen comercial de las reglas del sorteo para cabeceras y tarjetas.
        /// </summary>
        [Ignore]
        public string ResumenReglas => $"{OportunidadesTexto} | {CantidadNumerosTexto} | {CostoFormateado}";

        /// <summary>
        /// Periodo de vigencia formateado en dd/MM/yyyy.
        /// </summary>
        [Ignore]
        public string PeriodoTexto => $"{FechaInicio.ToString("dd/MM/yyyy", CulturaEsMx)} - {FechaFin.ToString("dd/MM/yyyy", CulturaEsMx)}";
    }
}

