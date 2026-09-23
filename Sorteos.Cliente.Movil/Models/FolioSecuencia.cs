using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que gestiona secuencias correlativas de folios unicos por serie o modulo.
    /// </summary>
    [Table("FolioSecuencia")]
    public class FolioSecuencia
    {
        /// <summary>
        /// Clave identificadora del consecutivo o serie (ej. "SORTEO").
        /// </summary>
        [PrimaryKey]
        public string Clave { get; set; } = "SORTEO";

        /// <summary>
        /// Ultimo numero correlativo emitido y reservado en la secuencia.
        /// </summary>
        public int UltimoFolio { get; set; } = 0;

        /// <summary>
        /// Marca de tiempo de la ultima actualizacion del contador.
        /// </summary>
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }
}

