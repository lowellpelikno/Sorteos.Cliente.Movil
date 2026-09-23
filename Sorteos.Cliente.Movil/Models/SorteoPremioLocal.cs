using SQLite;

namespace Sorteos.Cliente.Movil.Models
{
/// <summary>
    /// Entidad persistida en SQLite que modela la relacion de muchos a muchos entre un sorteo y los premios que oferta.
    /// </summary>
    [Table("SorteoPremioLocal")]
    public class SorteoPremioLocal
    {
        /// <summary>
        /// Identificador primario y autoincrementable del vinculo sorteo-premio.
        /// </summary>
        [PrimaryKey, AutoIncrement]
        public int IdSorteoPremio { get; set; }

        /// <summary>
        /// Identificador foraneo del sorteo.
        /// </summary>
        [Indexed]
        public int IdSorteo { get; set; }

        /// <summary>
        /// Identificador foraneo del premio adjudicado al sorteo.
        /// </summary>
        [Indexed]
        public int IdPremio { get; set; }
    }
}

